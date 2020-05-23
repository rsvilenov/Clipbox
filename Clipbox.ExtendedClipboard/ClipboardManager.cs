using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.Infrastructure.Persistence;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading.Tasks;
using System.Timers;

namespace Clipbox.ExtendedClipboard
{
    public sealed class ClipboardManager : IClipboardManager, IDisposable
    {
        private const int MAX_CLIPBOARD_ITEMS_TO_KEEP = 1000;

        // the maximum time an extension is allowed to lock the system clipboard
        private const int FORMAT_SUPPORT_EXTENSION_TIMEOUT = 1000;

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

        private IClipboardFormatExtensionContainer _formatExtensionContainer;

        private IPersistenceSupportExtensionContainer _persistenceExtensionContainer;

        private IClipboardEventProvider _clipboardEventProvider;

        private IPersistenceProvider _persistenceProvider;

        // TODO: this should be mock-able
        private SystemClipboard _systemClipboard;

        private PersistenceHelper _persistenceHelper;

        public event EventHandler<ItemFetchingEventArgs> ItemFetching;

        public event EventHandler<ItemStoringEventArgs> ItemStoring;

        public event EventHandler<ClipboardManagerEventArgs> ItemAdded;

        public event EventHandler<ClipboardManagerEventArgs> ItemRemoved;

        public event EventHandler<ClipboardManagerEventArgs> ItemChanged;

        private readonly object _ioLockHandle = new object();

        /// <summary>
        /// Creates an instance of ClipboardManager
        /// </summary>
        /// <param name="hWnd">Main program window's handle</param>
        public ClipboardManager(IClipboardEventProvider clipboardEventProvider,
            IClipboardFormatExtensionContainer formatExtensionContainer,
            IPersistenceSupportExtensionContainer persistenceExtensionContainer,
            IPersistenceProvider persistenceProvider)
        {
            _formatExtensionContainer = formatExtensionContainer;
            _persistenceExtensionContainer = persistenceExtensionContainer;
            _clipboardEventProvider = clipboardEventProvider;
            _persistenceProvider = persistenceProvider;

            _clipboardItems = new List<ClipboardItem>();
            _deletedSyncedClipboardItemIds = new List<string>();

            _systemClipboard = new SystemClipboard();
            _persistenceHelper = new PersistenceHelper(_persistenceExtensionContainer, _persistenceProvider);

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
                var formatExtension = _formatExtensionContainer.ResolveClipboardFormatSupport(cdo.ClipboardFormatName);
                if (formatExtension == null)
                {
                    // unsupported clipboard format
                    continue;
                }

                try
                {
                    if (!(formatExtension is IClipboardFormatSupport formatSupportExtension))
                    {
                        throw new InvalidCastException($"{formatExtension.GetType().Name} does not implement {nameof(IClipboardFormatSupport)}. This indicates a bug in the extension resolving code.");
                    }

                    // get a pointer to the clipboard item (HGLOBAL, HBITMAP, etc..)
                    IntPtr clipboardDataPtr = IntPtr.Zero;

                    try { clipboardDataPtr = formatSupportExtension.IntermediateFormatToPtr(cdo.IntermediateFormatObject); }
                    catch (Exception ex) { throw new ClipboxExtensionException(nameof(formatSupportExtension.IntermediateFormatToPtr), formatSupportExtension.GetType(), ex); }
                    
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
                            formatSupportExtension.OnClipboardFailure(clipboardDataPtr);
                        }
                        catch { }

                        throw;
                    }
                }
                finally
                {
                    formatExtension.Dispose();
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
            
            e.CancelPropagatingToOtherClipboardListeners = _isClipboardForwardingSuspended;

            var source = new ClipboardItemSource
            {
                SourceWindowInfo = GetClipboardItemSourceWindowInfo(),
                UserName = Environment.UserName,
                SourceMachineInfo = new SourceMachineInfo
                {
                    MachineName = Environment.MachineName,
                    MachineType = MachineType.PC,
                    MachineUniqueId = MachineUtils.GetMachineGuid()
                }
            };

            var itemFetchingEventArgs = new ItemFetchingEventArgs(source.SourceWindowInfo);

            ItemFetching?.Invoke(this, itemFetchingEventArgs);

            if (itemFetchingEventArgs.Cancel)
            {
                return;
            }

            var originalClipboardDataObjects = ReadClipboardDataObjectsFromSystemClipboard();
            
            // for some reason I receive WM_DRAWCLIPBOARD on pasting a file which has been cut the moment before
            // and GetAvailableFormats() returns zero items...
            if (!originalClipboardDataObjects.Any())
            {
                return;
            }

            // do not block this handler for too long
            Task.Run(() =>
            {
                var itemStoringArgs = new ItemStoringEventArgs(source.SourceWindowInfo,
                    originalClipboardDataObjects.Select(cdo => cdo.IntermediateFormatObject));

                ItemStoring?.Invoke(this, itemStoringArgs);

                if (itemStoringArgs.Cancel)
                {
                    return;
                }

                List<IClipboardDataObject> clipboardDataObjects = new List<IClipboardDataObject>();

                if (itemStoringArgs.AreIntermediateFormatsChanged)
                {
                    clipboardDataObjects.AddRange(GetChangedCdos(originalClipboardDataObjects, itemStoringArgs.IntermediateFormats));
                }
                else
                {
                    clipboardDataObjects.AddRange(originalClipboardDataObjects);
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


                ItemAdded?.Invoke(this, new ClipboardManagerEventArgs(currentItem.Id));
            });
        }

        private IEnumerable<IClipboardDataObject> GetChangedCdos(IEnumerable<IClipboardDataObject> originalCdos, IEnumerable<IIntermediateFormat> changedIntermediateFormatObjects)
        {
            foreach (var cdo in originalCdos)
            {
                IIntermediateFormat changedIntermediateFormat = changedIntermediateFormatObjects.Single(ifo => ifo.Metadata.FormatGuid == cdo.IntermediateFormatMetadata.FormatGuid);
                yield return new ClipboardDataObject(cdo.ClipboardFormatName, changedIntermediateFormat);
            }
        }

        /// <summary>
        /// Construct a SourceWindowInfo for the foreground window 
        /// </summary>
        /// <returns></returns>
        private SourceWindowInfo GetClipboardItemSourceWindowInfo()
        {
            bool isForemostWindowUsed = false;

            IntPtr lastClipboardUserHwnd = _systemClipboard.GetClipboardLastUserHWnd();

            IntPtr foremostWindowHwnd = WindowUtils.GetForemostWindowHwnd();

            if (lastClipboardUserHwnd == IntPtr.Zero)
            {
                isForemostWindowUsed = true;
                lastClipboardUserHwnd = foremostWindowHwnd;
            }

            SourceWindowInfo windowInfo = new SourceWindowInfo();

            using (WindowUtils.WindowInfo wInfo = WindowUtils.GetWindowInfo(lastClipboardUserHwnd))
            {
                bool isSourceSvcHost = wInfo.ApplicationPath.EndsWith("svchost.exe");
                if (!isForemostWindowUsed ||
                    /* some apps may use a service for clipboard access (it looks like "Paint 3d" is doing that) */
                    isSourceSvcHost)
                {
                    using (WindowUtils.WindowInfo foremostWInfo = WindowUtils.GetWindowInfo(foremostWindowHwnd))
                    {
                        if (isSourceSvcHost)
                        {
                            windowInfo.Icon = foremostWInfo.Icon?.ToByteArray();
                            windowInfo.Caption = foremostWInfo.Caption;
                            windowInfo.ApplicationInfo = GetApplicationInfo(foremostWInfo.ApplicationPath);
                            windowInfo.Icon = foremostWInfo.Icon?.ToByteArray();
                        }
                        else
                        {
                            // always prefer the title of the window in view
                            windowInfo.Caption =
                                foremostWInfo.ApplicationPath == wInfo.ApplicationPath ?
                                foremostWInfo.Caption
                                : wInfo.Caption;
                            windowInfo.ApplicationInfo = GetApplicationInfo(wInfo.ApplicationPath);
                            windowInfo.Icon = wInfo.Icon?.ToByteArray();
                        }
                    }
                }
                else
                {
                    windowInfo.Caption = wInfo.Caption;
                    windowInfo.ApplicationInfo = GetApplicationInfo(wInfo.ApplicationPath);
                    windowInfo.Icon = wInfo.Icon?.ToByteArray();
                }
                
                if (windowInfo.Icon == null)
                {
                    using (var icon = Icon.ExtractAssociatedIcon(wInfo.ApplicationPath))
                    using (var bmp = icon.ToBitmap())
                    {
                        windowInfo.Icon = bmp.ToByteArray();
                    }
                }
            }

            return windowInfo;
        }

        private SourceApplicationInfo GetApplicationInfo(string path)
        {
            try
            {
                if (ApplicationUtils.CheckIsWindowsApp(path))
                {
                    var windowsAppInfo = ApplicationUtils.GetAppInfo(path);
                    return new SourceApplicationInfo
                    {
                        ApplicationPath = path,
                        CompanyName = windowsAppInfo.ManifestData.PublisherDisplayName ?? windowsAppInfo.PackageData.Name,
                        FileDescription = windowsAppInfo.ManifestData.Description ?? windowsAppInfo.PackageData.Description,
                        ProductName = windowsAppInfo.ManifestData.DisplayName ?? windowsAppInfo.PackageData.Name,
                        ProductVersion = windowsAppInfo.ManifestData.Version
                    };
                }
                else
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
                    return new SourceApplicationInfo
                    {
                        ApplicationPath = path,
                        CompanyName = fileVersionInfo.CompanyName,
                        FileDescription = fileVersionInfo.FileDescription,
                        ProductName = fileVersionInfo.ProductName,
                        ProductVersion = fileVersionInfo.ProductVersion
                    };
                }
            }
            // this exception can happen even if the file exists, for example when we don't have permission to access it.
            //catch (FileNotFoundException)
            catch (Exception) // TODO: do better here...
            {
                return new SourceApplicationInfo
                {
                    ApplicationPath = path
                };
            }
        }

        /// <summary>
        /// Construct a SourceWindowInfo for the current process
        /// </summary>
        /// <returns></returns>
        private SourceWindowInfo GetOwnWindowInfo()
        {
            string applicationPath = Assembly.GetEntryAssembly().Location;
            
            return new SourceWindowInfo
            {
                ApplicationInfo = GetApplicationInfo(applicationPath),
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
                    var formatExtension = _formatExtensionContainer.ResolveClipboardFormatSupport(format.Value);
                    if (formatExtension == null)
                    {
                        // unsupported clipboard format
                        continue;
                    }

                    if (!(formatExtension is IClipboardFormatSupport formatSupportExtension))
                    {
                        throw new InvalidCastException($"{formatExtension.GetType().Name} does not implement {nameof(IClipboardFormatSupport)}. This indicates a bug in the extension resolving code.");
                    }


                    var clipboardDataPtr = _systemClipboard.GetClipboardData(format.Key);
                    if (clipboardDataPtr == IntPtr.Zero)
                    {
                        continue;
                    }

                    IIntermediateFormat intermediateFormatObj = null;

                    Task.Run(() =>
                    {
                        try
                        {
                            intermediateFormatObj = formatSupportExtension.PtrToIntermediateFormat(clipboardDataPtr);
                        }
                        catch (Exception ex) { throw new ClipboxExtensionException(nameof(formatSupportExtension.PtrToIntermediateFormat), formatSupportExtension.GetType(), ex); }

                    })
                    .Wait(FORMAT_SUPPORT_EXTENSION_TIMEOUT);

                    if (intermediateFormatObj != null)
                    {
                        if (intermediateFormatObj.Metadata == null)
                        {
                            throw new InvalidOperationException($"The intermediate format object for clipboard format {format.Key} does not have metadata set.");
                        }

                        yield return new ClipboardDataObject(clipboardFormatName: format.Value, intermediateFormatObject: intermediateFormatObj);
                    }
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

        public IReadOnlyList<IClipboardSyncObject> GetItemsForCloudSynchronization()
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
                                SyncAction.Update :
                                SyncAction.Create,
                            ModifiedDateUtcTicks = clipboardItem.TimestampInfo.ModifiedDateUtcTicks,
                            Data = transferableItem
                        });
                    }
                }

                syncObjects.AddRange(_deletedSyncedClipboardItemIds.Select(id => new ClipboardSyncObject
                {
                    Id = id,
                    Action = SyncAction.Delete
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
                UserName = Environment.UserName,
                SourceMachineInfo = new SourceMachineInfo
                {
                    MachineName = Environment.MachineName,
                    MachineType = MachineType.PC,
                    MachineUniqueId = MachineUtils.GetMachineGuid()
                }
            };

            var newClipboardDataObjects = new List<ClipboardDataObject>();

            foreach (IGeneratedClipboardDataObject generatedCdo in concatenated.ClipboardDataObjects)
            {
                // validate that we have a registered extension for the specified clipboard format
                var formatExtension = _formatExtensionContainer.ResolveClipboardFormatSupport(generatedCdo.ClipboardFormatName);
                if (formatExtension == null)
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
                clipboardItem.CanTransfer = filteredCdos.Any(cdo => cdo.IntermediateFormatMetadata.IsTransferable);

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

        public IReadOnlyList<long> GetAllCreationTimestampTicksUtc()
        {
            return _clipboardItems
                .Select(ci => ci.TimestampInfo.EntryDateUtcTicks)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyDictionary<IApplicationInfo, byte[]> GetAllSourceAppsAndIcons()
        {
            return _clipboardItems.Select(ci => ci.Source.SourceWindowInfo)
                .GroupBy(swi => swi.ApplicationInfo.ApplicationPath)
                .Select(g => g.First())
                .ToDictionary(k => k.ApplicationInfo, v => (byte[])v.Icon.Clone()); 
            // this is a newly created dictionary and modifying it does not affect the original values,
            // so there is no point in converting it to a ReadOnlyDictionary.
        }

        public IReadOnlyDictionary<IMachineInfo, string> GetAllSourceMachinesAndUsernames()
        {
            return _clipboardItems.Select(ci => ci.Source)
                .GroupBy(smi => new { smi.SourceMachineInfo.MachineUniqueId, smi.UserName })
                .Select(g => g.First())
                .ToDictionary(k => k.SourceMachineInfo, v => v.UserName);
        }

        #endregion
    }
}
