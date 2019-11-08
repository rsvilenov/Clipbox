using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Interfaces;
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
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Timers;

namespace Clipbox.ExtendedClipboard
{
    public sealed class ClipboardManager : IClipboardManager, IDisposable
    {
        private const int MAX_CLIPBOARD_ITEMS_TO_KEEP = 1000;

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

        private IPersistenceSupportExtensionContainer _persistenceExtentionContainer;

        private IClipboardEventProvider _clipboardEventProvider;

        // TODO: this should be mock-able
        private SystemClipboard _systemClipboard;

        private PersistenceHelper _persistenceHelper;

        public event EventHandler<ItemFetchingEventArgs> ItemFetching;

        public event EventHandler<ClipboardManagerEventArgs> ItemAdded;

        public event EventHandler<ClipboardManagerEventArgs> ItemRemoved;

        public event EventHandler<ClipboardManagerEventArgs> ItemChanged;

        private readonly object _ioLockHandle = new object();

        /// <summary>
        /// Creates an instance of ClipboardManager
        /// </summary>
        /// <param name="hWnd">Main program window's handle</param>
        public ClipboardManager(IClipboardEventProvider clipboardEventProvider,
            IClipboardFormatExtentionContainer formatExtentionContainer,
            IPersistenceSupportExtensionContainer persistenceExtentionContainer)
        {
            _formatExtentionContainer = formatExtentionContainer;
            _persistenceExtentionContainer = persistenceExtentionContainer;
            _clipboardEventProvider = clipboardEventProvider;

            _clipboardItems = new List<ClipboardItem>();
            _deletedSyncedClipboardItemIds = new List<string>();

            _systemClipboard = new SystemClipboard();
            _persistenceHelper = new PersistenceHelper(_persistenceExtentionContainer);

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
            foreach (ClipboardItem item in _persistenceHelper
                .ReadStoredClipboardItems(unshrink: false)
                .OrderByDescending(ci => ci.TimestampInfo.EntryDateTicks))
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
                var lastItem = _clipboardItems.OrderBy(ci => ci.TimestampInfo.EntryDateTicks).Last();
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
        public void RemoveClipboardItem(string id, bool beSilent = false, bool removeFromSystemClipboard = false)
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

                if (removeFromSystemClipboard && item.Id == _currentItemId)
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
                item = originalItem.ShallowClone();
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
                var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(cdo.ClipboardFormatName);
                if (formatExtention == null)
                {
                    // unsupported clipboard format
                    continue;
                }

                try
                {
                    if (!(formatExtention is IClipboardFormatSupport formatSupportExtention))
                    {
                        throw new InvalidCastException($"{formatExtention.GetType().Name} does not implement {nameof(IClipboardFormatSupport)}. This indicates a bug in the extension resolving code.");
                    }

                    // get a pointer to the clipboard item (HGLOBAL, HBITMAP, etc..)
                    IntPtr clipboardDataPtr = IntPtr.Zero;

                    try { clipboardDataPtr = formatSupportExtention.IntermediateFormatToPtr(cdo.IntermediateFormatObject); }
                    catch (Exception ex) { throw new ClipboxExtentionException(nameof(formatSupportExtention.IntermediateFormatToPtr), formatSupportExtention.GetType(), ex); }
                    
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
                        _systemClipboard.SetClipboardData(clipboardDataPtr, cdo.ClipboardFormatName);
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
                finally
                {
                    formatExtention.Dispose();
                }
            }

            _currentItemId = id;
        }

        public IEnumerable<string> GetAllItemIds(Func<IClipboardItem, bool> filterCondition = null)
        {
            lock (_ioLockHandle)
            {
                IEnumerable<ClipboardItem> allItems = _clipboardItems;
                if (filterCondition != null)
                {
                    // Func<IClipboardItem, bool> is not implicitly convertable to Func<ClipboardItem, bool>
                    // so we cannot pass filterCondition directly as an argument to the Where method.
                    allItems = _clipboardItems.Where(item => filterCondition(item));
                }

                return allItems.
                    OrderByDescending(item => item.TimestampInfo.EntryDateTicks).
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

                var clonned = internalItem.ShallowClone();
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
                MachineUniqueId = MachineUtils.GetMachineGuid(),
                UserName = Environment.UserName
            };

            var itemFetchingEventArgs = new ItemFetchingEventArgs(source.SourceWindowInfo);

            ItemFetching?.Invoke(this, itemFetchingEventArgs);

            if (itemFetchingEventArgs.Cancel)
            {
                return;
            }

            var clipboardDataObjects = ReadClipboardDataObjectsFromSystemClipboard();
            
            // for some reason I receive WM_DRAWCLIPBOARD on pasting a file which has been cut the moment before
            // and GetAvailableFormats() returns zero items...
            if (!clipboardDataObjects.Any())
            {
                return;
            }

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

        /// <summary>
        /// Construct a SourceWindowInfo for the foreground window 
        /// </summary>
        /// <returns></returns>
        private SourceWindowInfo GetForemostWindowInfo()
        {
            SourceWindowInfo windowInfo = new SourceWindowInfo();

            using (WindowUtils.WindowInfo wInfo = WindowUtils.GetForemostWindowInfo())
            {
                windowInfo.ApplicationPath = wInfo.ApplicationPath;
                windowInfo.Caption = wInfo.Caption;
                windowInfo.Icon = wInfo.Icon?.ToByteArray();
            }

            return windowInfo;
        }

        /// <summary>
        /// Construct a SourceWindowInfo for the current process
        /// </summary>
        /// <returns></returns>
        private SourceWindowInfo GetOwnWindowInfo()
        {
            return new SourceWindowInfo
            {
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Caption = ProductConstants.PRODUCT_NAME,
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
                    ?.ToBitmap()
                    ?.ToByteArray()
            };
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

                    if (!(formatExtention is IClipboardFormatSupport formatSupportExtention))
                    {
                        throw new InvalidCastException($"{formatExtention.GetType().Name} does not implement {nameof(IClipboardFormatSupport)}. This indicates a bug in the extension resolving code.");
                    }


                    var clipboardDataPtr = _systemClipboard.GetClipboardData(format.Key);
                    if (clipboardDataPtr == IntPtr.Zero)
                    {
                        continue;
                    }

                    IIntermediateFormat intermediateFormatObj = null;

                    try
                    {
                        intermediateFormatObj = formatSupportExtention.PtrToIntermediateFormat(clipboardDataPtr);
                    }
                    catch (Exception ex) { throw new ClipboxExtentionException(nameof(formatSupportExtention.PtrToIntermediateFormat), formatSupportExtention.GetType(), ex); }

                    yield return new ClipboardDataObject(clipboardFormatName: format.Value, intermediateFormatObject: intermediateFormatObj);
                }
            }
            finally
            {
                _systemClipboard.TryCloseAndUnlockClipboard();
            }
        }
        
#region Serialization
        

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
                    itemToUpdate.BeginEdit();
                    itemToUpdate.IsSynchronizedWithCloud = true;
                    itemToUpdate.EndEdit();

                    _persistenceHelper.StoreAndShrinkClipboardItem(itemToUpdate);
                }
            }
        }

        public bool TryAddItemFromCloud(byte[] itemData)
        {
            bool success = _persistenceHelper.TryGetClipboardItemFromCloudItemByteArray(itemData, out ClipboardItem item);
            if (success)
            {
                // TODO: add...
            }

            return success;
        }

        public IReadOnlyList<ClipboardSyncObject> GetItemsForCloudSynchronization()
        {
            // TODO: locking - delete and modify operations
            List<ClipboardSyncObject> syncObjects = new List<ClipboardSyncObject>();

            lock (_ioLockHandle)
            {
                foreach (var clipboardItem in _clipboardItems
                    .Where(item => !item.IsSynchronizedWithCloud 
                        && !item.IsTemporary 
                        && item.ShouldTransfer))
                {
                    byte[] transferableItem = _persistenceHelper.GetTransferableClipboardItemByteArray(clipboardItem);
                    if (transferableItem != null)
                    {
                        syncObjects.Add(new ClipboardSyncObject
                        {
                            Id = clipboardItem.Id,
                            Action = clipboardItem.IsModified ?
                                ClipboardSyncObject.SyncAction.Update :
                                ClipboardSyncObject.SyncAction.Create,
                            ModifiedDateUtcTicks = clipboardItem.TimestampInfo.ModifiedDateUtcTicks,
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

            // as a source, use the main window of Clipbox, if available
            var source = new ClipboardItemSource
            {
                SourceWindowInfo = GetOwnWindowInfo(),
                MachineName = Environment.MachineName,
                MachineUniqueId = MachineUtils.GetMachineGuid(),
                UserName = Environment.UserName
            };

            var newClipboardDataObjects = new List<ClipboardDataObject>();

            foreach (IGeneratedClipboardDataObject generatedCdo in concatenated.ClipboardDataObjects)
            {
                // validate that we have a registered extension for the specified clipboard format
                var formatExtention = _formatExtentionContainer.ResolveClipboardFormatSupport(generatedCdo.ClipboardFormatName);
                if (formatExtention == null)
                {
                    throw new InvalidOperationException($"Unsupported clipboard format: {generatedCdo.ClipboardFormatName}");
                }

                var cdo = new ClipboardDataObject(clipboardFormatName: generatedCdo.ClipboardFormatName, intermediateFormatObject: generatedCdo.IntermediateFormatObject);

                newClipboardDataObjects.Add(cdo);
            }

            var newItem = new ClipboardItem(source, newClipboardDataObjects);
            
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

        public void EnableDisableSyncOfItem(string clipboardItemId, bool enable)
        {
            lock (_ioLockHandle)
            {
                ClipboardItem item = _clipboardItems.Where(ci => ci.Id == clipboardItemId).FirstOrDefault();

                if (item == null)
                {
                    throw new ArgumentException($"ClipboardItem with Id {clipboardItemId} was not found.");
                }

                item.BeginEdit();
                // when we change the sync-ability a temporary item, it is no longer temporary
                // Todo: create a new non-temporary item instead.
                item.IsTemporary = false;

                item.ShouldTransfer = enable;

                item.EndEdit();

                _persistenceHelper.StoreAndShrinkClipboardItem(item);
            }
        }

        public void PinUnpinItemItem(string clipboardItemId, bool pin)
        {
            lock (_ioLockHandle)
            {
                ClipboardItem item = _clipboardItems.Where(ci => ci.Id == clipboardItemId).FirstOrDefault();

                if (item == null)
                {
                    throw new ArgumentException($"ClipboardItem with Id {clipboardItemId} was not found.");
                }

                item.BeginEdit();
                // when we pin a temporary item, it is no longer temporary
                item.IsTemporary = false;

                item.IsPinned = pin;

                item.EndEdit();

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

                var filteredCdos = clipboardItem.ClipboardDataObjects.Where((cdo, i) => !clipboardDataObjectIndices.Contains(i));
                if (!filteredCdos.Any())
                {
                    throw new ArgumentOutOfRangeException("No ClipboardDataObjects were found with the passed indices");
                }

                if (filteredCdos.Count() == clipboardItem.ClipboardDataObjects.Count())
                {
                    throw new InvalidOperationException("Removing all ClipboardDataObjects is not allowed.");
                }

                clipboardItem.BeginEdit();

                clipboardItem.ClipboardDataObjects = filteredCdos.ToList();
                clipboardItem.CanTransfer = filteredCdos.Any(cdo => cdo.IntermediateFormatObject?.IsTransferable == true);

                // invlidate the synchronized state of the item (it has changed)
                clipboardItem.IsSynchronizedWithCloud = false;

                clipboardItem.EndEdit();

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
