using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.ExtendedClipboard
{
    internal class PersistenceHelper
    {
        private const long MAX_CDO_SIZE = 1024 * 1024; // 1 MiB

        private const string APPDATA_CACHE_SUBDIR = "ClipboardDataCache";

        private IClipboardFormatExtentionContainer _formatExtentionContainer;

        public PersistenceHelper(IClipboardFormatExtentionContainer clipboardFormatExtentionContainer)
        {
            _formatExtentionContainer = clipboardFormatExtentionContainer;
        }
        
        public void StoreAndShrinkClipboardItem(ClipboardItem clipboardItem)
        {
            if (clipboardItem.IsShrinked)
            {
                throw new ArgumentException($"ClipboardItem with id{clipboardItem.Id} has already been shrinked. Please restore it first.");
            }

            // generate persistable clipboard item
            PersistableClipboardItem persistableClipboardItem = GeneratePersistableClipboardItem(clipboardItem);
            if (persistableClipboardItem == null)
            {
                // the item is not persistable
                return;
            }

            bool anyCdoShrinked = false;

            // shrink the original item if necessary
            foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
            {
                if (persistableCdo.ByteArray.LongLength > MAX_CDO_SIZE)
                {
                    ClipboardDataObject originalCdo = (ClipboardDataObject)clipboardItem.ClipboardDataObjects.First(cdo => cdo.Format == persistableCdo.Format);
                    originalCdo.OriginalData = null;
                    anyCdoShrinked = true;
#if DEBUG
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
#endif
                }
            }

            clipboardItem.IsShrinked = anyCdoShrinked;

            EnsureAppDataCacheSubdirExists();
            // save the persistable item to disk
            AppDataUtils.WriteToApplicationData(Path.Combine(APPDATA_CACHE_SUBDIR, clipboardItem.Id),
                SerializeObject(persistableClipboardItem));

        }

        public void RestoreShrinkedClipboardItem(ClipboardItem clipboardItem)
        {
            EnsureAppDataCacheSubdirExists();

            // load from disk
            byte[] clipboardItemBytes = AppDataUtils.ReadBytesFromApplicationData(Path.Combine(APPDATA_CACHE_SUBDIR, clipboardItem.Id));
            PersistableClipboardItem persistableClipboardItem = DeserializeObject<PersistableClipboardItem>(clipboardItemBytes);

            bool anyCdoRestored = false;

            foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
            {
                // find the original clipboard data object
                ClipboardDataObject originalCdo = (ClipboardDataObject)clipboardItem.ClipboardDataObjects.FirstOrDefault(cdo => cdo.Format == persistableCdo.Format);
                if (originalCdo != null)
                {
                    originalCdo.ByteArray = persistableCdo.ByteArray;
                    originalCdo.OriginalData = GetOriginalData(originalCdo.Format, originalCdo.ByteArray);
                    anyCdoRestored = true;
                }
            }

            clipboardItem.IsShrinked = !anyCdoRestored;
        }

        public IEnumerable<ClipboardItem> ReadStoredClipboardItems(bool unshrink = false)
        {
            EnsureAppDataCacheSubdirExists();

            string cachePath = Path.Combine(AppDataUtils.GetAppDataDir(true), APPDATA_CACHE_SUBDIR);
            foreach (string fileName in Directory.GetFiles(cachePath))
            {
                string clipboardItemId = Path.GetFileNameWithoutExtension(fileName);
                var clipboardDataObjects = new List<ClipboardDataObject>();

                PersistableClipboardItem persistableClipboardItem = null;

                try
                {
                    // load from disk
                    byte[] clipboardItemBytes = AppDataUtils.ReadBytesFromApplicationData(Path.Combine(APPDATA_CACHE_SUBDIR, clipboardItemId));
                    persistableClipboardItem = DeserializeObject<PersistableClipboardItem>(clipboardItemBytes);
                }
                catch
                {
                    // ignore the files that could not be read/deserialized
                    try { DeleteClipboardItemFromStorage(clipboardItemId); }
                    catch { }

                    continue;
                }

                bool anyShrinkedCdoRestored = false;
                // restore the clipboard data objects
                foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
                {
                    ClipboardDataObject originalCdo = new ClipboardDataObject
                    {
                        Format = persistableCdo.Format,
                        ContentClass = persistableCdo.ContentClass,
                        IsTransferable = persistableCdo.IsTransferable,
                        FormatExtentionName = persistableCdo.FormatExtentionName,
                        FormatExtentionVersion = persistableCdo.FormatExtentionVersion
                    };

                    if (unshrink)
                    {
                        originalCdo.ByteArray = persistableCdo.ByteArray;
                        originalCdo.OriginalData = GetOriginalData(originalCdo.Format, originalCdo.ByteArray);
                        anyShrinkedCdoRestored = true;
                    }

                    clipboardDataObjects.Add(originalCdo);
                }

                yield return new ClipboardItem(clipboardItemId, persistableClipboardItem.Source, clipboardDataObjects)
                {
                    IsShrinked = !anyShrinkedCdoRestored,
                    IsPinned = persistableClipboardItem.IsPinned,
                    EntryDateTicks = persistableClipboardItem.EntryDateTicks,
                    EntryDateUtcTicks = persistableClipboardItem.EntryDateUtcTicks,
                    ModifiedDateTicks = persistableClipboardItem.ModifiedDateTicks,
                    ModifiedDateUtcTicks = persistableClipboardItem.ModifiedDateUtcTicks
                };
            }  
        }

        public void DeleteClipboardItemFromStorage(string clipboardItemId)
        {
            string filePath = Path.Combine
                (
                    AppDataUtils.GetAppDataDir(true),
                    Path.Combine(APPDATA_CACHE_SUBDIR, clipboardItemId)
                );

            // TODO: make sure this plays well with FileLocker, used by AppDataUtils file access operations
            File.Delete(filePath);
            File.Delete($"{filePath}.lock");
        }

        /// <summary>
        /// Gets a transferable clipboard item (or null if the file is not transferable)
        /// </summary>
        /// <param name="clipboardItem"></param>
        /// <returns>TransferableClipboardItem on success; null if the IClipboardItem is not transferable</returns>
        public byte[] GetTransferableClipboardItemByteArray(ClipboardItem clipboardItem)
        {
            if (clipboardItem.IsShrinked)
            {
                throw new ArgumentException($"ClipboardItem with id{clipboardItem.Id} has already been shrinked. Please restore it first.");
            }

            var persistableClipboardItem = GeneratePersistableClipboardItem(clipboardItem);
            if (persistableClipboardItem == null)
            {
                // the item is not persistable
                return null;
            }

            var transferableClipboardItem =  GenerateTransferableClipboardItem(persistableClipboardItem);
            if (transferableClipboardItem == null)
            {
                // the item is not transferable
                return null;
            }

            return Compress(SerializeObject(transferableClipboardItem));
        }

        public ClipboardItem GetClipboardItemFromCloudItemByteArray(byte[] itemData)
        {
            var transferableClipboardItem = DeserializeObject<TransferableClipboardItem>(Decompress(itemData));

            var clipboardDataObjects = transferableClipboardItem.TransferableClipboardDataObjects.Select
                (tcdo => new ClipboardDataObject
                {
                    ByteArray = tcdo.ByteArray,
                    ContentClass = tcdo.ContentClass,
                    Format = tcdo.Format,
                    FormatExtentionName = tcdo.FormatExtentionName,
                    FormatExtentionVersion = tcdo.FormatExtentionVersion,
                    IsTransferable = true
                });

            var clipboardItem = new ClipboardItem(transferableClipboardItem.Id, transferableClipboardItem.Source, clipboardDataObjects)
            {
                IsSynchronizedWithCloud = true,
                EntryDateTicks = transferableClipboardItem.EntryDateTicks,
                EntryDateUtcTicks = transferableClipboardItem.EntryDateUtcTicks,
                ModifiedDateTicks = transferableClipboardItem.ModifiedDateTicks,
                ModifiedDateUtcTicks = transferableClipboardItem.ModifiedDateUtcTicks
            };

            return clipboardItem;
        }

        private byte[] SerializeObject(object item)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, item);
                ms.Flush();
                return ms.ToArray();
            }
        }

        private T DeserializeObject<T>(byte[] itemBytes)
        {
            using (MemoryStream ms = new MemoryStream(itemBytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
        
        [HandleProcessCorruptedStateExceptions]
        private object GetOriginalData(string clipboardFormat, byte[] byteArray)
        {
            object originalData = null;

            var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(clipboardFormat);
            if (formatExtention != null && formatExtention is IPersistableFormat)
            {
                var persistableFormatExtention = (IPersistableFormat)formatExtention;

                try
                {
                    originalData = persistableFormatExtention.ByteArrayToObject(byteArray);
                }
                catch (Exception ex)
                {
                    throw new ClipboxExtentionException(nameof(persistableFormatExtention.ByteArrayToObject), persistableFormatExtention.GetType(), ex);
                }
            }

            return originalData;
        }

        private PersistableClipboardItem GeneratePersistableClipboardItem(ClipboardItem clipboardItem)
        {
            var persistableCdos = GeneratePersistableClipboardDataObjects(clipboardItem.ClipboardDataObjects.Cast<ClipboardDataObject>()).ToList();
            if (persistableCdos.Any())
            {
                return new PersistableClipboardItem
                {
                    Tag = clipboardItem.Tag,
                    Source = clipboardItem.Source,
                    Id = clipboardItem.Id,
                    IsPinned = clipboardItem.IsPinned,
                    PersistableClipboardDataObjects = persistableCdos,
                    EntryDateTicks = clipboardItem.EntryDateTicks,
                    EntryDateUtcTicks = clipboardItem.EntryDateUtcTicks,
                    ModifiedDateTicks = clipboardItem.ModifiedDateTicks,
                    ModifiedDateUtcTicks = clipboardItem.ModifiedDateUtcTicks
                };
            }

            return null;
        }

        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data))
            using (MemoryStream output = new MemoryStream())
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
                return output.ToArray();
            }
        }

        private TransferableClipboardItem GenerateTransferableClipboardItem(PersistableClipboardItem persistableClipboardItem)
        {
            var transferableCdos = GenerateTransferableClipboardDataObject(persistableClipboardItem.PersistableClipboardDataObjects).
                // make sure that we do not pass an iterator to the serializer, as it is known to have problems with iterators...
                ToList();

            if (transferableCdos.Any())
            {
                return new TransferableClipboardItem
                {
                    Tag = persistableClipboardItem.Tag,
                    Source = persistableClipboardItem.Source,
                    Id = persistableClipboardItem.Id,
                    IsPinned = persistableClipboardItem.IsPinned,
                    TransferableClipboardDataObjects = transferableCdos,
                    EntryDateTicks = persistableClipboardItem.EntryDateTicks,
                    EntryDateUtcTicks = persistableClipboardItem.EntryDateUtcTicks,
                    ModifiedDateTicks = persistableClipboardItem.ModifiedDateTicks,
                    ModifiedDateUtcTicks = persistableClipboardItem.ModifiedDateUtcTicks
                };
            }

            return null;
        }

        private IEnumerable<TransferableClipboardDataObject> GenerateTransferableClipboardDataObject(IEnumerable<PersistableClipboardDataObject> persistableClipboardDataObjects)
        {
            return persistableClipboardDataObjects.
                Where(cdo => cdo.IsTransferable).
                Select(cdo => new TransferableClipboardDataObject
                {
                    ByteArray = cdo.ByteArray,
                    ContentClass = cdo.ContentClass,
                    Format = cdo.Format,
                    FormatExtentionName = cdo.FormatExtentionName,
                    FormatExtentionVersion = cdo.FormatExtentionVersion
                });
        }
        
        [HandleProcessCorruptedStateExceptions]
        private IEnumerable<PersistableClipboardDataObject> GeneratePersistableClipboardDataObjects(IEnumerable<ClipboardDataObject> clipboardDataObjects)
        {
            foreach (ClipboardDataObject cdo in clipboardDataObjects)
            {
                var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(cdo.Format);
                if (formatExtention == null)
                {
                    throw new InvalidOperationException("No suitable clipboard format support extention found, therefore serialization is not possible.");
                }

                if (formatExtention is IPersistableFormat)
                {
                    var persistableFormatExtention = formatExtention as IPersistableFormat;
                    byte[] dataBytes = null;
                    object clipboardData = cdo.OriginalData;

                    try { dataBytes = persistableFormatExtention.ObjectToByteArray(ref clipboardData); }
                    catch (Exception ex) { throw new ClipboxExtentionException(nameof(persistableFormatExtention.ObjectToByteArray), persistableFormatExtention.GetType(), ex); }

                    if (dataBytes == null)
                    {
                        continue;
                    }

                    yield return new PersistableClipboardDataObject
                    {
                        Format = cdo.Format,
                        ContentClass = cdo.ContentClass,
                        FormatExtentionName = cdo.FormatExtentionName,
                        FormatExtentionVersion = cdo.FormatExtentionVersion,
                        ByteArray = dataBytes,
                        // check whether the contents can be transferred to another computer...
                        IsTransferable = !persistableFormatExtention.RequiresSameMachine
                    };
                }

            }
        }

        private void EnsureAppDataCacheSubdirExists()
        {
            string cachePath = Path.Combine(AppDataUtils.GetAppDataDir(throwIfNotExists: false), APPDATA_CACHE_SUBDIR);
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
        }
    }
}
