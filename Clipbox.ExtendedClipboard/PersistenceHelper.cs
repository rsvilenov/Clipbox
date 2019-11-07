using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.Infrastructure.Compression;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.ExtendedClipboard
{
    internal class PersistenceHelper
    {
        private const long MAX_CDO_SIZE = 1024 * 1024; // 1 MiB

        private const string APPDATA_CACHE_SUBDIR = "ClipboardDataCache";

        private IPersistenceSupportExtensionContainer _persistenceExtentionContainer;

        public PersistenceHelper(IPersistenceSupportExtensionContainer persistenceSupportExtentionContainer)
        {
            _persistenceExtentionContainer = persistenceSupportExtentionContainer;
        }
        
        public void StoreAndShrinkClipboardItem(ClipboardItem clipboardItem)
        {
            if (!CheckIsClipboardItemPersistable(clipboardItem))
            {
                // the item is not persistable
                return;
            }

            // generate persistable clipboard item
            PersistableClipboardItem persistableClipboardItem = GeneratePersistableClipboardItem(clipboardItem);

            // if the item is persistable and is modified, then we should be
            // able to load it from the disk. If the item's modified ticks is
            // different, we should proceed with saving. Otherwise - we stop here.
            if (clipboardItem.IsModified)
            {
                // load from disk
                PersistableClipboardItem savedPersistableClipboardItem = LoadPersistableClipboardItemFromDisk(clipboardItem);
                
                // check to see if it has really been modified
                if (persistableClipboardItem.TimestampInfo.ModifiedDateUtcTicks == savedPersistableClipboardItem.TimestampInfo.ModifiedDateUtcTicks)
                {
                    return;
                }

                // load the original clipboard data objects in order to prevent the
                // possibility of someone modifying the object somewhere accidentally
                // or with bad intentions in mind and then we save it here. CDOs should
                // not be modifiable.
                RestoreClipboardDataObjects(clipboardItem, persistableClipboardItem);
            }

            // shrink the original item if necessary
            foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
            {
                if (persistableCdo.ByteArray.LongLength > MAX_CDO_SIZE)
                {
                    ClipboardDataObject originalCdo = (ClipboardDataObject)clipboardItem.ClipboardDataObjects.First(cdo => cdo.ClipboardFormatName == persistableCdo.ClipboardFormatName);
                    originalCdo.IntermediateFormatObject = null;
#if DEBUG
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
#endif
                }
            }

            EnsureAppDataCacheSubdirExists();
            // save the persistable item to disk
            AppDataUtils.WriteToApplicationData(Path.Combine(APPDATA_CACHE_SUBDIR, clipboardItem.Id),
                SerializeObject(persistableClipboardItem));

        }

        public void RestoreShrinkedClipboardItem(ClipboardItem clipboardItem)
        {
            EnsureAppDataCacheSubdirExists();
            PersistableClipboardItem persistableClipboardItem = LoadPersistableClipboardItemFromDisk(clipboardItem);
            RestoreClipboardDataObjects(clipboardItem, persistableClipboardItem);
        }

        private void RestoreClipboardDataObjects(ClipboardItem clipboardItem, PersistableClipboardItem persistableClipboardItem)
        {
            foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
            {
                // find the original clipboard data object
                ClipboardDataObject originalCdo = (ClipboardDataObject)clipboardItem.ClipboardDataObjects.FirstOrDefault(cdo => cdo.ClipboardFormatName == persistableCdo.ClipboardFormatName);
                if (originalCdo != null)
                {
                    if (TryGetCdoFromPersistableCdo(persistableCdo, out ClipboardDataObject persistedClipboardDataObject))
                    {
                        originalCdo.IntermediateFormatObject = persistedClipboardDataObject.IntermediateFormatObject;
                    }
                }
            }
        }

        private PersistableClipboardItem LoadPersistableClipboardItemFromDisk(ClipboardItem clipboardItem)
        {
            string itemFilePath = Path.Combine(APPDATA_CACHE_SUBDIR, clipboardItem.Id);
            if (!AppDataUtils.CheckFileExists(itemFilePath))
            {
                throw new ArgumentException($"No stored data can be found for clipboard item id {clipboardItem.Id}");
            }

            // load from disk
            byte[] clipboardItemBytes = AppDataUtils.ReadBytesFromApplicationData(itemFilePath);
            PersistableClipboardItem persistableClipboardItem = DeserializeObject<PersistableClipboardItem>(clipboardItemBytes);
            return persistableClipboardItem;
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
                
                // restore the clipboard data objects
                foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
                {
                    ClipboardDataObject originalCdo = new ClipboardDataObject
                    {
                        ClipboardFormatName = persistableCdo.ClipboardFormatName
                    };

                    if (unshrink)
                    {
                        if (TryGetCdoFromPersistableCdo(persistableCdo, out ClipboardDataObject clipboardDataObject))
                        {
                            originalCdo.IntermediateFormatObject = clipboardDataObject.IntermediateFormatObject;
                        }
                    }

                    clipboardDataObjects.Add(originalCdo);
                }

                TimestampInfo tsInfo = new TimestampInfo(persistableClipboardItem.TimestampInfo);
                
                yield return new ClipboardItem(clipboardItemId, persistableClipboardItem.Source, clipboardDataObjects, tsInfo)
                {
                    IsPinned = persistableClipboardItem.IsPinned,
                    CanTransfer = persistableClipboardItem.CanTransfer,
                    ShouldTransfer = persistableClipboardItem.ShouldTransfer
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
                // restore a copy of the item
                clipboardItem = clipboardItem.ShallowClone();
                RestoreShrinkedClipboardItem(clipboardItem);
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

            return ByteArrayCompressor.Compress(SerializeObject(transferableClipboardItem));
        }

        private PersistableClipboardDataObject GetPersistableCdoFromTransferableCdo(TransferableClipboardDataObject transferableCdo)
        {
            return new PersistableClipboardDataObject
            {
                ByteArray = transferableCdo.ByteArray,
                ClipboardFormatName = transferableCdo.ClipboardFormatName,
                ContentClass = transferableCdo.ContentClass,
                IntermediateFormatGuid = transferableCdo.IntermediateFormatGuid,
                IsTransferable = true
            };
        }

        [HandleProcessCorruptedStateExceptions]
        private bool TryGetCdoFromPersistableCdo(PersistableClipboardDataObject persistableCdo, out ClipboardDataObject clipboardDataObject)
        {
            clipboardDataObject = null;

            var persistenceExtention = _persistenceExtentionContainer.ResolvePersistenceSupport(persistableCdo.IntermediateFormatGuid);
            if (persistenceExtention == null)
            {
                throw new InvalidOperationException($"No suitable persistence support extention found for {persistableCdo.IntermediateFormatGuid}, therefore deserialization is not possible.");
            }

            IIntermediateFormat intermediateFormatObj = null;

            try { intermediateFormatObj = persistenceExtention.ByteArrayToIntermediateFormat(persistableCdo.ByteArray); }
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(persistenceExtention.ByteArrayToIntermediateFormat), persistenceExtention.GetType(), ex); }

            bool deserializationSuccess = intermediateFormatObj != null;

            if (deserializationSuccess)
            {
                clipboardDataObject = new ClipboardDataObject
                {
                    IntermediateFormatObject = intermediateFormatObj,
                    ClipboardFormatName = persistableCdo.ClipboardFormatName
                };
            }

            return deserializationSuccess;
        }

        public bool TryGetClipboardItemFromCloudItemByteArray(byte[] itemData, out ClipboardItem clipboardItem)
        {
            clipboardItem = null;

            var transferableClipboardItem = DeserializeObject<TransferableClipboardItem>(ByteArrayCompressor.Decompress(itemData));

            List<ClipboardDataObject> clipboardDataObjects = new List<ClipboardDataObject>();

            foreach (TransferableClipboardDataObject tdto in transferableClipboardItem.TransferableClipboardDataObjects)
            {
                PersistableClipboardDataObject persistableCdo = GetPersistableCdoFromTransferableCdo(tdto);
                if (TryGetCdoFromPersistableCdo(persistableCdo, out ClipboardDataObject clipboardDataObject))
                {
                    clipboardDataObjects.Add(clipboardDataObject);
                }
            }

            bool hasSuccessfllyRestoredCdos = clipboardDataObjects.Any();

            if (hasSuccessfllyRestoredCdos)
            {
                TimestampInfo tsInfo = new TimestampInfo(transferableClipboardItem.TimestampInfo);

                clipboardItem = new ClipboardItem(transferableClipboardItem.Id, transferableClipboardItem.Source, clipboardDataObjects, tsInfo)
                {
                    IsSynchronizedWithCloud = true,
                    CanTransfer = false,
                    ShouldTransfer = false
                };
            }

            return hasSuccessfllyRestoredCdos;
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
                    TimestampInfo = clipboardItem.TimestampInfo,
                    CanTransfer = clipboardItem.CanTransfer,
                    ShouldTransfer = clipboardItem.ShouldTransfer
                };
            }

            return null;
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
                    TimestampInfo = persistableClipboardItem.TimestampInfo
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
                    ClipboardFormatName = cdo.ClipboardFormatName,
                    IntermediateFormatGuid = cdo.IntermediateFormatGuid
                });
        }

        private bool CheckIsClipboardItemPersistable(ClipboardItem item)
        {
            if (!item.ClipboardDataObjects.Any())
            {
                throw new ArgumentException($"{nameof(item.ClipboardDataObjects)} is empty.");
            }

            return item.ClipboardDataObjects.Any(cdo => 
            _persistenceExtentionContainer.CheckIsIntermediateFormatSupported(cdo.IntermediateFormatObject.FormatGuid));
            
        }


        [HandleProcessCorruptedStateExceptions]
        private IEnumerable<PersistableClipboardDataObject> GeneratePersistableClipboardDataObjects(IEnumerable<ClipboardDataObject> clipboardDataObjects)
        {
            foreach (ClipboardDataObject cdo in clipboardDataObjects)
            {
                var persistenceExtention = _persistenceExtentionContainer.ResolvePersistenceSupport(cdo.IntermediateFormatObject.FormatGuid);
                if (persistenceExtention == null)
                {
                    continue;
                }

                byte[] dataBytes = null;

                try { dataBytes = persistenceExtention.IntermediateFormatToByteArray(cdo.IntermediateFormatObject); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(persistenceExtention.IntermediateFormatToByteArray), persistenceExtention.GetType(), ex); }

                if (dataBytes == null)
                {
                    continue;
                }

                yield return new PersistableClipboardDataObject
                {
                    ClipboardFormatName = cdo.ClipboardFormatName,
                    ContentClass = cdo.IntermediateFormatObject.ContentClass,
                    IntermediateFormatGuid = cdo.IntermediateFormatObject.FormatGuid,
                    ByteArray = dataBytes,
                    // check whether the contents can be transferred to another computer...
                    IsTransferable = cdo.IntermediateFormatObject.IsTransferable
                };

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
