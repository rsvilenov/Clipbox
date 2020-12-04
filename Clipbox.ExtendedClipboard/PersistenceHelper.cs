using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.Infrastructure.Compression;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace Clipbox.ExtendedClipboard
{
    internal class PersistenceHelper
    {
        private const long MAX_CDO_SIZE = 1024 * 1024; // 1 MiB

        /// <summary>
        /// The category under which the implementation of IPersistenceProvider will store our data.
        /// </summary>
        private const string CACHE_PERSISTENCE_CATEGORY = "ClipboardDataCache";
        private const string CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY = "ClipboardManagerData";
        private const string DELETED_SYNCED_CLIPBOARD_ITEM_IDS_KEY = "DeletedSyncedClipboardItemIds";


        private readonly IPersistenceSupportExtensionContainer _persistenceExtensionContainer;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly IEncryptionProvider _encryptionProvider;

        public PersistenceHelper(IPersistenceSupportExtensionContainer persistenceSupportExtensionContainer, 
            IPersistenceProvider persistenceProvider, 
            IEncryptionProvider encryptionProvider)
        {
            _persistenceExtensionContainer = persistenceSupportExtensionContainer;
            _persistenceProvider = persistenceProvider;
            _encryptionProvider = encryptionProvider;

            if (!_persistenceProvider.CheckIfCategoryExists(CACHE_PERSISTENCE_CATEGORY))
            {
                _persistenceProvider.CreateCategory(CACHE_PERSISTENCE_CATEGORY);
            }

            if (!_persistenceProvider.CheckIfCategoryExists(CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY))
            {
                _persistenceProvider.CreateCategory(CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY);
            }
        }
        
        public void StoreAndShrinkClipboardItem(ClipboardItem clipboardItem, bool checkIsItemModified = true)
        {
            if (!CheckIsClipboardItemPersistable(clipboardItem))
            {
                // the item is not persistable
                return;
            }

            // the item may not be shrinked (it may come for example from the cloud)
            // so we should check that before restoring it needlessly.
            if (clipboardItem.IsShrinked)
            {
                if (checkIsItemModified)
                {
                    // if the item is persistable and is modified, then we should be
                    // able to load it from the disk. If the item's modified ticks is
                    // different, we should proceed with saving. Otherwise - we stop here.
                    if (clipboardItem.IsModified)
                    {
                        // load from disk
                        PersistableClipboardItem savedClipboardItem = LoadPersistableClipboardItemFromStorage(clipboardItem);

                        // check to see if it has really been modified
                        if (clipboardItem.TimestampInfo.ModifiedDateUtcTicks == savedClipboardItem.TimestampInfo.ModifiedDateUtcTicks)
                        {
                            return;
                        }

                        // load the original clipboard data objects in order to prevent the
                        // possibility of someone modifying the object somewhere accidentally
                        // or with bad intentions in mind and then we save it here. CDOs should
                        // not be modifiable.
                        RestoreClipboardDataObjects(clipboardItem, savedClipboardItem, decryptIfPossible: true);
                    }
                }
                else
                {
                    // if the caller has requested that we should not check
                    // whether the item has been modified before saving,
                    // then restore the item from the disk directly.
                    PersistableClipboardItem savedClipboardItem = LoadPersistableClipboardItemFromStorage(clipboardItem);
                    RestoreClipboardDataObjects(clipboardItem, savedClipboardItem, decryptIfPossible: true);
                }
            }

            // generate persistable clipboard item
            PersistableClipboardItem persistableClipboardItem = GeneratePersistableClipboardItem(clipboardItem, encryptIfPossible: true);
            

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

            // store the persistable item
            _persistenceProvider.Write(clipboardItem.Id, ByteArrayCompressor.Compress(BinaryFormatterSerialization.SerializeObject(persistableClipboardItem)), CACHE_PERSISTENCE_CATEGORY);
        }

        public void RestoreShrinkedClipboardItem(ClipboardItem clipboardItem)
        {
            PersistableClipboardItem persistableClipboardItem = LoadPersistableClipboardItemFromStorage(clipboardItem);
            RestoreClipboardDataObjects(clipboardItem, persistableClipboardItem, decryptIfPossible: true);
        }

        private void RestoreClipboardDataObjects(ClipboardItem clipboardItem, PersistableClipboardItem persistableClipboardItem, bool decryptIfPossible)
        {
            foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
            {
                // find the original clipboard data object
                ClipboardDataObject originalCdo = (ClipboardDataObject)clipboardItem.ClipboardDataObjects.FirstOrDefault(cdo => cdo.ClipboardFormatName == persistableCdo.ClipboardFormatName);
                if (originalCdo != null)
                {
                    if (TryGetCdoFromPersistableCdo(persistableCdo, out ClipboardDataObject persistedClipboardDataObject, decryptIfPossible))
                    {
                        originalCdo.IntermediateFormatObject = persistedClipboardDataObject.IntermediateFormatObject;
                    }
                }
            }
        }

        private PersistableClipboardItem LoadPersistableClipboardItemFromStorage(ClipboardItem clipboardItem)
        {
            string itemFilePath = Path.Combine(CACHE_PERSISTENCE_CATEGORY, clipboardItem.Id);
            if (!_persistenceProvider.CheckIfKeyExists(clipboardItem.Id, CACHE_PERSISTENCE_CATEGORY))
            {
                throw new ArgumentException($"No stored data can be found for clipboard item id {clipboardItem.Id}");
            }

            // load from storage
            byte[] clipboardItemBytes = _persistenceProvider.ReadBytes(clipboardItem.Id, CACHE_PERSISTENCE_CATEGORY);
            PersistableClipboardItem persistableClipboardItem = BinaryFormatterSerialization.DeserializeObject<PersistableClipboardItem>(ByteArrayCompressor.Decompress(clipboardItemBytes));
            return persistableClipboardItem;
        }

        public IEnumerable<ClipboardItem> ReadStoredClipboardItems(bool unshrink = false)
        {
            foreach (string persistenceKey in _persistenceProvider.ListKeys(CACHE_PERSISTENCE_CATEGORY))
            {
                var clipboardDataObjects = new List<ClipboardDataObject>();

                PersistableClipboardItem persistableClipboardItem = null;

                try
                {
                    // load from storage
                    byte[] clipboardItemBytes = _persistenceProvider.ReadBytes(persistenceKey, CACHE_PERSISTENCE_CATEGORY);
                    persistableClipboardItem = BinaryFormatterSerialization.DeserializeObject<PersistableClipboardItem>(ByteArrayCompressor.Decompress(clipboardItemBytes));
                }
                catch
                {
                    // ignore the files that could not be read/deserialized
                    try { _persistenceProvider.Delete(persistenceKey, CACHE_PERSISTENCE_CATEGORY); }
                    catch { }

                    continue;
                }

                // restore the clipboard data objects
                foreach (PersistableClipboardDataObject persistableCdo in persistableClipboardItem.PersistableClipboardDataObjects)
                {
                    ClipboardDataObject originalCdo = null;


                    if (unshrink)
                    {
                        if (TryGetCdoFromPersistableCdo(persistableCdo, out ClipboardDataObject restoredCdo, decryptIfPossible: true))
                        {
                            originalCdo = new ClipboardDataObject(restoredCdo.ClipboardFormatName, restoredCdo.IntermediateFormatObject);
                        }
                    }
                    else
                    {
                        originalCdo = new ClipboardDataObject(persistableCdo.ClipboardFormatName, persistableCdo.IntermediateFormatMetadata);
                    }
                    
                    clipboardDataObjects.Add(originalCdo);
                }
                
                TimestampInfo tsInfo = new TimestampInfo(persistableClipboardItem.TimestampInfo);

                yield return new ClipboardItem(id: persistenceKey,
                    source: persistableClipboardItem.Source,
                    clipboardDataObjects: clipboardDataObjects,
                    timestampInfo: tsInfo)
                {
                    IsPinned = persistableClipboardItem.IsPinned,
                    IsProtected = persistableClipboardItem.IsProtected,
                    CanTransfer = persistableClipboardItem.CanTransfer,
                    ShouldTransfer = persistableClipboardItem.ShouldTransfer,
                    IsSynchronizedWithCloud = persistableClipboardItem.IsSynchronizedWithCloud
                };
            }
        }

        public void DeleteClipboardItemFromStorage(string clipboardItemId)
        {
            _persistenceProvider.Delete(key: clipboardItemId, category: CACHE_PERSISTENCE_CATEGORY);
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

            var persistableClipboardItem = GeneratePersistableClipboardItem(clipboardItem, encryptIfPossible: false);
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

            return ByteArrayCompressor.Compress(BinaryFormatterSerialization.SerializeObject(transferableClipboardItem));
        }

        private PersistableClipboardDataObject GetPersistableCdoFromTransferableCdo(TransferableClipboardDataObject transferableCdo)
        {
            return new PersistableClipboardDataObject
            {
                ByteArray = transferableCdo.ByteArray,
                ClipboardFormatName = transferableCdo.ClipboardFormatName,
                IntermediateFormatMetadata = new SerializableIntermediateFormatMetadata(transferableCdo.IntermediateFormatMetadata),
                IsTransferable = true
            };
        }

        [HandleProcessCorruptedStateExceptions]
        private bool TryGetCdoFromPersistableCdo(PersistableClipboardDataObject persistableCdo, out ClipboardDataObject clipboardDataObject, bool decryptIfPossible)
        {
            clipboardDataObject = null;

            var persistenceExtension = _persistenceExtensionContainer.ResolvePersistenceSupport(persistableCdo.IntermediateFormatMetadata.FormatGuid);
            if (persistenceExtension == null)
            {
                throw new InvalidOperationException($"No suitable persistence support extension found for {persistableCdo.IntermediateFormatMetadata.FormatGuid}, therefore deserialization is not possible.");
            }

            IIntermediateFormat intermediateFormatObj = null;

            byte[] serializedData = null;
            if (persistenceExtension.EncryptData && decryptIfPossible)
            {
                serializedData = _encryptionProvider.Decrypt(persistableCdo.ByteArray);
            }
            else
            {
                serializedData = persistableCdo.ByteArray;
            }

            try { intermediateFormatObj = persistenceExtension.ByteArrayToIntermediateFormat(serializedData); }
            catch (Exception ex) { throw new ClipboxExtensionException(nameof(persistenceExtension.ByteArrayToIntermediateFormat), persistenceExtension.GetType(), ex); }

            bool deserializationSuccess = intermediateFormatObj != null;

            if (deserializationSuccess)
            {
                clipboardDataObject = new ClipboardDataObject(clipboardFormatName: persistableCdo.ClipboardFormatName, intermediateFormatObject: intermediateFormatObj);
            }

            return deserializationSuccess;
        }

        public bool TryGetClipboardItemFromCloudItemByteArray(byte[] itemData, out ClipboardItem clipboardItem)
        {
            clipboardItem = null;

            TransferableClipboardItem transferableClipboardItem = null;
            try
            {
                transferableClipboardItem = BinaryFormatterSerialization.DeserializeObject<TransferableClipboardItem>(ByteArrayCompressor.Decompress(itemData));
            }
            catch (SerializationException)
            {
                // TODO: what to do here? just ignore? but what about errors, caused by contract inconsistency?
                Debug.WriteLine("error deserializing a cloud item");
                return false;
            }

            List<ClipboardDataObject> clipboardDataObjects = new List<ClipboardDataObject>();

            foreach (TransferableClipboardDataObject tdto in transferableClipboardItem.TransferableClipboardDataObjects)
            {
                PersistableClipboardDataObject persistableCdo = GetPersistableCdoFromTransferableCdo(tdto);
                if (TryGetCdoFromPersistableCdo(persistableCdo, out ClipboardDataObject clipboardDataObject, decryptIfPossible: false))
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
        
        private PersistableClipboardItem GeneratePersistableClipboardItem(ClipboardItem clipboardItem, bool encryptIfPossible)
        {
            var persistableCdos = GeneratePersistableClipboardDataObjects(clipboardItem.ClipboardDataObjects.Cast<ClipboardDataObject>(), encryptIfPossible).ToList();
            if (persistableCdos.Any())
            {
                return new PersistableClipboardItem
                {
                    Tag = clipboardItem.Tag,
                    Source = clipboardItem.Source,
                    Id = clipboardItem.Id,
                    IsPinned = clipboardItem.IsPinned,
                    IsProtected = clipboardItem.IsProtected,
                    PersistableClipboardDataObjects = persistableCdos,
                    TimestampInfo = clipboardItem.TimestampInfo,
                    CanTransfer = clipboardItem.CanTransfer,
                    ShouldTransfer = clipboardItem.ShouldTransfer,
                    IsSynchronizedWithCloud = clipboardItem.IsSynchronizedWithCloud
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
                    ClipboardFormatName = cdo.ClipboardFormatName,
                    IntermediateFormatMetadata = new SerializableIntermediateFormatMetadata(cdo.IntermediateFormatMetadata)
                });
        }

        private bool CheckIsClipboardItemPersistable(ClipboardItem item)
        {
            if (!item.ClipboardDataObjects.Any())
            {
                throw new ArgumentException($"{nameof(item.ClipboardDataObjects)} is empty.");
            }

            return item.ClipboardDataObjects.OfType<ClipboardDataObject>().Any(cdo => 
            _persistenceExtensionContainer.CheckIsIntermediateFormatSupported(cdo.IntermediateFormatMetadata.FormatGuid));
            
        }


        [HandleProcessCorruptedStateExceptions]
        private IEnumerable<PersistableClipboardDataObject> GeneratePersistableClipboardDataObjects(IEnumerable<ClipboardDataObject> clipboardDataObjects, bool encryptIfPossible)
        {
            foreach (ClipboardDataObject cdo in clipboardDataObjects)
            {
                var persistenceExtension = _persistenceExtensionContainer.ResolvePersistenceSupport(cdo.IntermediateFormatMetadata.FormatGuid);
                if (persistenceExtension == null)
                {
                    continue;
                }

                byte[] dataBytes = null;

                try { dataBytes = persistenceExtension.IntermediateFormatToByteArray(cdo.IntermediateFormatObject); }
                catch (Exception ex) { throw new ClipboxExtensionException(nameof(persistenceExtension.IntermediateFormatToByteArray), persistenceExtension.GetType(), ex); }

                if (dataBytes == null)
                {
                    continue;
                }

                byte[] finalByteArray = null;
                if (persistenceExtension.EncryptData && encryptIfPossible)
                {
                    finalByteArray = _encryptionProvider.Encrypt(dataBytes);
                }
                else
                {
                    finalByteArray = dataBytes;
                }

                yield return new PersistableClipboardDataObject
                {
                    ClipboardFormatName = cdo.ClipboardFormatName,
                    IntermediateFormatMetadata = new SerializableIntermediateFormatMetadata(cdo.IntermediateFormatMetadata),
                    ByteArray = dataBytes,
                    IsTransferable = cdo.IntermediateFormatMetadata.IsTransferable
                };

            }
        }

        public void StoreDeletedSyncedClipboardItemIds(List<string> itemIds)
        {
            _persistenceProvider.Write(DELETED_SYNCED_CLIPBOARD_ITEM_IDS_KEY, ByteArrayCompressor.Compress(BinaryFormatterSerialization.SerializeObject(itemIds)), CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY);
        }

        public List<string> ReadDeletedSyncedClipboardItemIds()
        {
            try
            {
                if (_persistenceProvider.CheckIfKeyExists(DELETED_SYNCED_CLIPBOARD_ITEM_IDS_KEY, CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY))
                {
                    byte[] compressedBytes = _persistenceProvider.ReadBytes(DELETED_SYNCED_CLIPBOARD_ITEM_IDS_KEY, CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY);
                    return BinaryFormatterSerialization.DeserializeObject<List<string>>(ByteArrayCompressor.Decompress(compressedBytes));
                }
            }
            catch (SerializationException)
            {
                // ignore the deserialization error and delete the record
                try { _persistenceProvider.Delete(CLIPBOARD_MANAGER_DATA_PERSISTENCE_CATEGORY, DELETED_SYNCED_CLIPBOARD_ITEM_IDS_KEY); }
                catch { }
            }

            return null;
        }
    }
}
