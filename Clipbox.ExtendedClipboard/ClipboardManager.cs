using Clipbox.Core.ClipboardContent;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Timers;

namespace Clipbox.ExtendedClipboard
{
    public sealed class ClipboardManager : IClipboardManager, IDisposable
    {
        private const int MAX_CLIPBOARD_ITEMS_TO_KEEP = 35;

#if DEBUG
        private const int CLIPBOARD_LISTENING_SUSPEND_INTERVAL_MS = 3000;
#else
        private const int CLIPBOARD_LISTENING_SUSPEND_INTERVAL_MS = 50;
#endif    

        private bool _isClipboardForwardingSuspended;

        private bool _isClipboardListeningSuspended;

        private Timer _clipboardListeningSuspTimer;

        private List<ClipboardItem> _clipboardItems;

        private List<string> _deletedSyncedClipboardItemIds;

        private string _currentItemId;

        private IClipboardFormatExtentionContainer _formatExtentionContainer;

        private IClipboardEventProvider _clipboardEventProvider;

        // TODO: this should be mock-able
        private SystemClipboard _systemClipboard;

        private PersistenceHelper _persistenceHelper;

        public event EventHandler<ItemFetchingEventArgs> ItemFetching;

        public event EventHandler<ClipboardManagerEventArgs> ItemAdded;

        public event EventHandler<ClipboardManagerEventArgs> ItemRemoved;

        public event EventHandler<ClipboardManagerEventArgs> ItemChanged;

        private object _ioLockHandle = new object();

        /// <summary>
        /// Creates an instance of ClipboardManager
        /// </summary>
        /// <param name="hWnd">Main program window's handle</param>
        public ClipboardManager(IClipboardEventProvider clipboardEventProvider, IClipboardFormatExtentionContainer formatExtentionContainer)
        {
            _formatExtentionContainer = formatExtentionContainer;
            _clipboardEventProvider = clipboardEventProvider;

            _clipboardItems = new List<ClipboardItem>();
            _deletedSyncedClipboardItemIds = new List<string>();

            _systemClipboard = new SystemClipboard();
            _persistenceHelper = new PersistenceHelper(_formatExtentionContainer);

            LoadFromDisk();

            _clipboardListeningSuspTimer = new Timer(CLIPBOARD_LISTENING_SUSPEND_INTERVAL_MS);
            _clipboardListeningSuspTimer.AutoReset = false;
            _clipboardListeningSuspTimer.Elapsed += (sender, args) =>
                _isClipboardListeningSuspended = false;

            _clipboardEventProvider.ClipboardContentChanged += _interceptor_ClipboardContentChanged;
        }

        private void LoadFromDisk()
        {
            int itemNumber = 0;
            foreach (ClipboardItem item in _persistenceHelper.ReadStoredClipboardItems(unshrink: false))
            {
                if (++itemNumber > MAX_CLIPBOARD_ITEMS_TO_KEEP)
                {
                    _persistenceHelper.DeleteClipboardItemFromStorage(item.Id);
                }
                else
                {
                    _clipboardItems.Add(item);
                }
            }
        }
        
        /// <summary>
        /// Do not let other applications know that a clipboard change has occurred
        /// </summary>
        public void SuspendClipboardForwarding()
        {
            _isClipboardForwardingSuspended = true;
        }

        /// <summary>
        /// Let other applications know that a clipboard change has occurred
        /// </summary>
        public void ResumeClipboardForwarding()
        {
            _isClipboardForwardingSuspended = false;
        }

        /// <summary>
        /// Remove the last item
        /// </summary>
        public void DiscardLastClipboardItem()
        {
            if (_clipboardItems.Any())
            {
                var lastItem = _clipboardItems.First();
                RemoveClipboardItem(lastItem.Id);
            }
        }

        /// <summary>
        /// Clear the system clipboard
        /// </summary>
        public void ClearSystemClipboard()
        {
            // temporarily suspend the clipboard listening
            _isClipboardListeningSuspended = true;
            // temporary suspend clipboard forwarding,
            // in order to prevent other applications from
            // receiving an event that the clipboard contents has changed
            _isClipboardForwardingSuspended = true;
            _clipboardListeningSuspTimer.Start();

            _systemClipboard.EmptyClipboard();

            _currentItemId = null;
        }

        /// <summary>
        /// Remove a clipboard item
        /// </summary>
        /// <param name="item"></param>
        public void RemoveClipboardItem(string id, bool beSilent = false)
        {
            ClipboardItem item = null;

            lock (_ioLockHandle)
            {
                item = _clipboardItems.FirstOrDefault(it => it.Id == id);
                if (item == null)
                {
                    throw new ArgumentException($"Could not find any item with this id: {id}");
                }

                _clipboardItems.Remove(item);
                if (item.Id == _currentItemId)
                {
                    ClearSystemClipboard();
                }

                if (!item.IsTemporary)
                {
                    _persistenceHelper.DeleteClipboardItemFromStorage(item.Id);

                    if (item.IsSynchronizedWithCloud)
                    {
                        _deletedSyncedClipboardItemIds.Add(item.Id);
                    }
                }
            }

            if (!beSilent)
            {
                ItemRemoved?.Invoke(this, new ClipboardManagerEventArgs(item.Id));
            }
        }

        /// <summary>
        /// Put the item to the system clipboard
        /// </summary>
        /// <param name="item"></param>
        // TODO: set items by key, not by object. The world should be able to only read the objects, not change them.
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public void SetCurrentItem(string id)
        {
            ClipboardItem item = null;
            
            lock (_ioLockHandle)
            {
                ClipboardItem originalItem = _clipboardItems.FirstOrDefault(it => it.Id == id);
                item = CloneClipboardItem(originalItem);
                if (item.IsShrinked)
                {
                    _persistenceHelper.RestoreShrinkedClipboardItem(item);
                }
            }

            if (item == null)
            {
                throw new ArgumentException($"Could not find any item with this id: {id}");
            }

            ClearSystemClipboard();

            for (int i = 0; i < item.ClipboardDataObjects.Count; i++)
            {
                ClipboardDataObject cdo = item.ClipboardDataObjects[i] as ClipboardDataObject;
                var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(cdo.Format);
                if (formatExtention == null)
                {
                    // unsupported clipboard format
                    continue;
                }

                try
                {
                    if (formatExtention is ClipboardFormatSupport)
                    {
                        var formatSupportExtention = formatExtention as ClipboardFormatSupport;

                        // get a pointer to the clipboard item (HGLOBAL, HBITMAP, etc..)
                        object originalData = cdo.OriginalData;
                        IntPtr clipboardDataPtr = IntPtr.Zero;

                        try { clipboardDataPtr = formatSupportExtention.DataToPtr(ref originalData); }
                        catch (Exception ex) { throw new ClipboxExtentionException(nameof(formatSupportExtention.DataToPtr), formatSupportExtention.GetType(), ex); }

                        // IMPORTANT!!!
                        // the data may have changed in DataToPtr()
                        // this is the case for example with Metafiles.
                        
                        if (cdo.OriginalData != originalData)
                        {
                            cdo.OriginalData = originalData;
                        }

                        // We need to update the original clipboard item as well.
                        ClipboardDataObject originalCdo = item.ClipboardDataObjects[i] as ClipboardDataObject;
                        if (originalCdo.OriginalData != null && originalCdo.OriginalData != originalData)
                        {
                            originalCdo.OriginalData = originalData;
                        }

                        if (clipboardDataPtr == IntPtr.Zero)
                        {
                            continue;
                        }

                        // temporarily suspend the clipboard listening
                        _isClipboardListeningSuspended = true;
                        // temporary suspend clipboard forwarding,
                        // in order to prevent other applications from
                        // receiving an event that the clipboard contents has changed
                        _isClipboardForwardingSuspended = true;
                        _clipboardListeningSuspTimer.Start();

                        // pass the pointer to the data to the system clipboard
                        try
                        {
                            _systemClipboard.SetClipboardData(clipboardDataPtr, cdo.Format);
                        }
                        catch 
                        // free the handle only on failure. Otherwise, the clipboard
                        // takes ownership of it
                        {
                            try
                            {
                                // OnClipboardFailure, unless overridden, calls GlobalFree() [kernel32]
                                formatSupportExtention.OnClipboardFailure(clipboardDataPtr);
                            }
                            catch { }

                            throw;
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"This format extention type is not supported yet. Type: {formatExtention} - clipboard format: {cdo.Format}");
                    }
                }
                finally
                {
                    formatExtention.Dispose();
                }
            }

            _currentItemId = id;
        }

        public IEnumerable<string> GetAllItemIds(bool excludeShrinkedItems = false)
        {
            lock (_ioLockHandle)
            {
                IEnumerable<ClipboardItem> allItems = _clipboardItems;
                if (excludeShrinkedItems)
                {
                    allItems = _clipboardItems.Where(item => !item.IsShrinked);
                }

                return allItems.
                    OrderByDescending(item => item.EntryDateTicks).
                    Select(item => item.Id);
            }
        }

        public IClipboardItem GetClipboardItem(string id, bool restoreShrinked = false)
        {
            lock (_ioLockHandle)
            {
                ClipboardItem internalItem = _clipboardItems.FirstOrDefault(item => item.Id == id);
                if (internalItem == null)
                {
                    throw new ArgumentException($"No item with id {id} was found.");
                }

                var clonned = CloneClipboardItem(internalItem);
                if (restoreShrinked && internalItem.IsShrinked)
                {
                    _persistenceHelper.RestoreShrinkedClipboardItem(clonned);
                }

                return clonned;
            }
        }
        
        public void Clear()
        {
            lock (_ioLockHandle)
            {
                List<string> idsToRemove = _clipboardItems
                    .Select(item => item.Id)
                    // do not return an iterator, as we are going to modify the source collection
                    .ToList();
                foreach (string id in idsToRemove)
                {
                    RemoveClipboardItem(id, beSilent: false);
                }
            }

            ClearSystemClipboard();
        }
        
        private void _interceptor_ClipboardContentChanged(object sender, ClipboardEventArgs e)
        {
            if (_isClipboardListeningSuspended)
            {
                return;
            }

            var source = new ClipboardItemSource
            {
                SourceWindowInfo = GetForemostWindowInfo(),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName
            };

            var itemFetchingEventArgs = new ItemFetchingEventArgs(source.SourceWindowInfo);

            ItemFetching?.Invoke(this, itemFetchingEventArgs);

            if (itemFetchingEventArgs.Cancel)
            {
                return;
            }

            var clipboardDataObjects = ReadClipboardDataObjectsFromSystemClipboard();
            var currentItem = new ClipboardItem(source, clipboardDataObjects);

            lock (_ioLockHandle)
            {
                _clipboardItems.Add(currentItem);

                if (_clipboardItems.Count > MAX_CLIPBOARD_ITEMS_TO_KEEP)
                {
                    DiscardLastClipboardItem();
                }

                // try to save memory by swapping the big data to the hard drive
                _persistenceHelper.StoreAndShrinkClipboardItem(currentItem);
            }

            e.CancelPropagatingToOtherClipboardListeners = _isClipboardForwardingSuspended;
            
            ItemAdded?.Invoke(this, new ClipboardManagerEventArgs(currentItem.Id));
        }

        private SourceWindowInfo GetForemostWindowInfo()
        {
            SourceWindowInfo windowInfo = new SourceWindowInfo();

            using (WindowUtils.WindowInfo wInfo = WindowUtils.GetForemostWindowInfo())
            {
                windowInfo.ApplicationPath = wInfo.ApplicationPath;
                windowInfo.Caption = wInfo.Caption;
                windowInfo.Icon = wInfo.Icon.ToByteArray();
            }

            return windowInfo;
        }

        

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private IEnumerable<IClipboardDataObject> ReadClipboardDataObjectsFromSystemClipboard()
        {
            _systemClipboard.TryLockClipboardOpen();

            try
            {

                foreach (KeyValuePair<uint, string> format in _systemClipboard.GetAvailableFormats())
                {
                    var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(format.Value);
                    if (formatExtention == null)
                    {
                        // unsupported clipboard format
                        continue;
                    }

                    if (formatExtention is ClipboardFormatSupport)
                    {
                        var formatSupportExtention = formatExtention as ClipboardFormatSupport;

                        var clipboardDataPtr = _systemClipboard.GetClipboardData(format.Key);
                        if (clipboardDataPtr == IntPtr.Zero)
                        {
                            continue;
                        }

                        object clipboardData = null;
                        ClipboardContentClass contentClass = default(ClipboardContentClass);

                        try
                        {
                            clipboardData = formatSupportExtention.PtrToData(clipboardDataPtr);
                            contentClass = formatSupportExtention.ContentClass;
                        }
                        catch (Exception ex) { throw new ClipboxExtentionException(nameof(formatSupportExtention.PtrToData), formatSupportExtention.GetType(), ex); }

                        IExtentionInfo extentionInfo = ((IExtentionInfoProvider)_formatExtentionContainer).GetExtentionInfo(formatSupportExtention);

                        yield return new ClipboardDataObject
                        {
                            Format = format.Value,
                            OriginalData = clipboardData,
                            ContentClass = contentClass,
                            FormatExtentionName = formatSupportExtention.GetType().Name, // TODO: is name sufficient?
                            FormatExtentionVersion = extentionInfo.Version
                        };
                    }
                }
            }
            finally
            {
                _systemClipboard.TryCloseAndUnlockClipboard();
            }
        }
        
#region Serialization
        
        

        private ClipboardItem CloneClipboardItem(ClipboardItem item)
        {
            ClipboardItem clonnedItem = null;
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, item);
                ms.Position = 0;

                clonnedItem = (ClipboardItem)formatter.Deserialize(ms);
            }

            // clone the members marked with [NonSerialized]

            clonnedItem.ClipboardDataObjects = new List<IClipboardDataObject>();
            foreach (ClipboardDataObject cdo in item.ClipboardDataObjects)
            {
                clonnedItem.ClipboardDataObjects.Add(new ClipboardDataObject
                {
                    ByteArray = cdo.ByteArray,
                    ContentClass = cdo.ContentClass,
                    Format = cdo.Format,
                    FormatExtentionName = cdo.FormatExtentionName,
                    FormatExtentionVersion = cdo.FormatExtentionVersion,
                    IsTransferable = cdo.IsTransferable,
                    OriginalData = cdo.OriginalData
                });
            }

            return clonnedItem;
        }

        public void SetItemSynchronized(string id)
        {
            if (_deletedSyncedClipboardItemIds.Contains(id))
            {
                _deletedSyncedClipboardItemIds.Remove(id);
            }
            else
            {
                var itemToUpdate = _clipboardItems.FirstOrDefault(item => item.Id == id);
                if (itemToUpdate != null)
                {
                    itemToUpdate.IsSynchronizedWithCloud = true;
                }
            }
        }

        public void AddItemFromCloud(byte[] itemData)
        {
            ClipboardItem item = _persistenceHelper.GetClipboardItemFromCloudItemByteArray(itemData);

            foreach (ClipboardDataObject cdo in item.ClipboardDataObjects)
            {
                var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(cdo.Format);
                if (formatExtention == null)
                {
                    // unsupported clipboard format
                    return;
                }

                IExtentionInfo extentionInfo = ((IExtentionInfoProvider)_formatExtentionContainer).GetExtentionInfo(formatExtention);
                if (!(formatExtention.GetType().Name == cdo.FormatExtentionName && extentionInfo.Version >= cdo.FormatExtentionVersion))
                {
                    // the cdo was generated with another extention
                    // or with a newer version of the same extention,
                    // so we cannot guarantee compatibility
                    return;
                }

                if (formatExtention is ClipboardFormatSupport)
                {
                    var formatSupportExtention = formatExtention as ClipboardFormatSupport;
                    
                    if (formatSupportExtention is IPersistableFormat)
                    {
                        var persistableFormatExtention = formatSupportExtention as IPersistableFormat;
                        
                        try { cdo.OriginalData = persistableFormatExtention.ByteArrayToObject(cdo.ByteArray); }
                        catch (Exception ex) { throw new ClipboxExtentionException(nameof(persistableFormatExtention.ObjectToByteArray), persistableFormatExtention.GetType(), ex); }
                    }

                }
            }
        }

        public IReadOnlyList<ClipboardSyncObject> GetItemsForCloudSynchronization()
        {
            // TODO: locking - delete and modify operations
            List<ClipboardSyncObject> syncObjects = new List<ClipboardSyncObject>();

            lock (_ioLockHandle)
            {
                foreach (var clipboardItem in _clipboardItems.Where(item => !item.IsSynchronizedWithCloud && !item.IsTemporary))
                {
                    // restore the data of the items so that they will be stored properly when updated
                    if (clipboardItem.IsShrinked)
                    {
                        _persistenceHelper.RestoreShrinkedClipboardItem(clipboardItem);
                    }

                    byte[] transferableItem = _persistenceHelper.GetTransferableClipboardItemByteArray(clipboardItem);
                    if (transferableItem != null)
                    {
                        syncObjects.Add(new ClipboardSyncObject
                        {
                            Id = clipboardItem.Id,
                            Action = clipboardItem.IsModified ?
                                ClipboardSyncObject.SyncAction.Update :
                                ClipboardSyncObject.SyncAction.Create,
                            ModifiedDateUtcTicks = clipboardItem.ModifiedDateUtcTicks,
                            Data = transferableItem
                        });
                    }
                }

                syncObjects.AddRange(_deletedSyncedClipboardItemIds.Select(id => new ClipboardSyncObject
                {
                    Id = id,
                    Action = ClipboardSyncObject.SyncAction.Delete
                }));
            }

            return syncObjects.AsReadOnly();
        }
        
#endregion

        public void Dispose()
        {
            _systemClipboard.Dispose();
        }

        public IClipboardItem AppendFromGenerated(IGeneratedClipboardItem concatenated, bool beSilent = false, bool isTemporary = false)
        {
            if (concatenated == null)
            {
                throw new ArgumentNullException(nameof(concatenated));
            }

            if (concatenated.ClipboardDataObjects == null || ! concatenated.ClipboardDataObjects.Any())
            {
                throw new ArgumentException($"{nameof(concatenated.ClipboardDataObjects)} does not contain any objects");
            }

            var newClipboardDataObjects = new List<ClipboardDataObject>();

            foreach (IGeneratedClipboardDataObject generatedCdo in concatenated.ClipboardDataObjects)
            {
                var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(generatedCdo.Format);
                if (formatExtention == null)
                {
                    // unsupported clipboard format
                    return null;
                }

                if (formatExtention is ClipboardFormatSupport)
                {
                    var formatSupportExtention = formatExtention as ClipboardFormatSupport;

                    IExtentionInfo extentionInfo = ((IExtentionInfoProvider)_formatExtentionContainer).GetExtentionInfo(formatSupportExtention);

                    object clipboardData = generatedCdo.Data;

                    var cdo = new ClipboardDataObject
                    {
                        Format = generatedCdo.Format,
                        OriginalData = generatedCdo.Data,
                        ContentClass = generatedCdo.ContentClass,
                        FormatExtentionName = formatSupportExtention.GetType().Name,
                        FormatExtentionVersion = extentionInfo.Version
                    };

                    if (formatSupportExtention is IPersistableFormat)
                    {
                        var persistableFormatExtention = formatSupportExtention as IPersistableFormat;
                        byte[] dataBytes = null;

                        try { dataBytes = persistableFormatExtention.ObjectToByteArray(ref clipboardData); }
                        catch (Exception ex) { throw new ClipboxExtentionException(nameof(persistableFormatExtention.ObjectToByteArray), persistableFormatExtention.GetType(), ex); }

                        cdo.ByteArray = dataBytes;
                        // check whether the contents can be transferred to another computer...
                        cdo.IsTransferable = !persistableFormatExtention.RequiresSameMachine;
                    }

                    newClipboardDataObjects.Add(cdo);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            var newItem = new ClipboardItem(null, newClipboardDataObjects);
            
            lock (_ioLockHandle)
            {
                _clipboardItems.Add(newItem);

                if (isTemporary)
                {
                    newItem.IsTemporary = true;
                }
                else
                {
                    // try to save memory by swapping the big data to the hard drive
                    _persistenceHelper.StoreAndShrinkClipboardItem(newItem);
                }
            }

            if (!beSilent)
            {
                ItemAdded?.Invoke(this, new ClipboardManagerEventArgs(newItem.Id));
            }

            return newItem;
        }

        #region Edit methods

        public void PinUnpinItemItem(string clipboardItemId, bool pin)
        {
            lock (_ioLockHandle)
            {
                ClipboardItem item = _clipboardItems.Where(ci => ci.Id == clipboardItemId).FirstOrDefault();

                if (item == null)
                {
                    throw new ArgumentException($"ClipboardItem with Id {clipboardItemId} was not found.");
                }

                // when we pin a temporary item, it is no longer temporary
                item.IsTemporary = false;

                if (item.IsShrinked)
                {
                    _persistenceHelper.RestoreShrinkedClipboardItem(item);
                }

                item.IsPinned = pin;

                _persistenceHelper.StoreAndShrinkClipboardItem(item);
            }
        }

        public void RemoveClipbpardDataObjects(string clipboardItemId, int[] clipboardDataObjectIndices, bool beSilend = false)
        {
            lock (_ioLockHandle)
            {
                var clipboardItem = _clipboardItems.FirstOrDefault(item => item.Id == clipboardItemId);
                if (clipboardItemId == null)
                {
                    throw new ArgumentException($"{nameof(clipboardItemId)} {clipboardItemId} was not found.");
                }

                if (clipboardDataObjectIndices.Length >= clipboardItem.ClipboardDataObjects.Count())
                {
                    throw new ArgumentOutOfRangeException("The number of passed indices is bigger than or equal to the number of ClipboardDataObjects.");
                }

                if (clipboardItem.IsShrinked)
                {
                    // restore the data of the items so that they will be stored properly when updated
                    _persistenceHelper.RestoreShrinkedClipboardItem(clipboardItem);
                }

                var filteredCdos = clipboardItem.ClipboardDataObjects.Where((cdo, i) => !clipboardDataObjectIndices.Contains(i));
                if (!filteredCdos.Any())
                {
                    throw new ArgumentOutOfRangeException("No ClipboardDataObjects were found with the passed indices");
                }

                if (filteredCdos.Count() == clipboardItem.ClipboardDataObjects.Count())
                {
                    throw new InvalidOperationException("Removing all ClipboardDataObjects is not allowed.");
                }

                clipboardItem.ClipboardDataObjects = filteredCdos.ToList();

                // invlidate the synchronized state of the item (it has changed)
                clipboardItem.IsSynchronizedWithCloud = false;

                clipboardItem.IsModified = true;
                clipboardItem.ModifiedDateTicks = DateTime.Now.Ticks;
                clipboardItem.ModifiedDateUtcTicks = DateTime.UtcNow.Ticks;

                // update the stored clipboard item
                _persistenceHelper.StoreAndShrinkClipboardItem(clipboardItem);
            }

            if (!beSilend)
            {
                ItemChanged?.Invoke(this, new ClipboardManagerEventArgs(clipboardItemId));
            }
        }

        #endregion
    }
}
