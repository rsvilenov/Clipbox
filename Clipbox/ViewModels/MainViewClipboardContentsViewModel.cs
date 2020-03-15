using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ViewModels
{
    public class MainViewClipboardContentsViewModel : ClipboardContentsViewModelBase
    {
        private readonly ISearchService _searchService;
        private readonly ICloudSynchronizationService _cloudSyncService;


        public MainViewClipboardContentsViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService,
            ISearchService searchService,
            ICloudSynchronizationService cloudSyncService) 
                : base(clipboardManager, previewGenerationService, settingsService)
        {
            _searchService = searchService;
            _cloudSyncService = cloudSyncService;

            _cloudSyncService.AuthenticationStateChanged += _cloudSyncService_AuthenticationStateChanged;
            _cloudSyncService.ItemSynced += _cloudSyncService_ItemSynced;

            IsCloudSyncAvailable = CheckIsCloudServiceAvailable();
        }
        
        private void _cloudSyncService_ItemSynced(object sender, Services.EventArgsClasses.SyncEventArgs e)
        {
            var wrapper = ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == e.ClipboardItemId);
            if (wrapper != null)
            {
                wrapper.SyncStatus = CloudSyncStatus.SyncComplete;
            }
        }

        private void _cloudSyncService_AuthenticationStateChanged(object sender, System.EventArgs e)
        {
            IsCloudSyncAvailable = CheckIsCloudServiceAvailable();
            if (IsCloudSyncAvailable)
            {
                foreach (var wrapper in ClipboardViewItemWrappers)
                {
                    if (wrapper != null
                        && IsCloudSyncAvailable
                        && wrapper.SyncStatus == CloudSyncStatus.SyncRequested)
                    {
                        wrapper.SyncStatus = CloudSyncStatus.Syncing;
                    }
                }
            }
            else
            {
                foreach (var wrapper in ClipboardViewItemWrappers)
                {
                    if (wrapper != null
                        && IsCloudSyncAvailable
                        && wrapper.SyncStatus == CloudSyncStatus.Syncing)
                    {
                        wrapper.SyncStatus = CloudSyncStatus.SyncRequested;
                    }
                }
            }
        }

        private bool CheckIsCloudServiceAvailable()
        {
            return _cloudSyncService.IsEnabled
                && _cloudSyncService.IsNetworkAvailable 
                && _cloudSyncService.IsAuthenticated;
        }

        private bool _isCloudSyncAvailable;

        public bool IsCloudSyncAvailable
        {
            get { return _isCloudSyncAvailable; }
            set { SetField(ref _isCloudSyncAvailable, value); }
        }

        protected override void OnSyncOfClipboardEntryEnabledOrDisabled(string clipboardItemId)
        {
            var wrapper = ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == clipboardItemId);
            if (wrapper != null 
                && IsCloudSyncAvailable
                && wrapper.SyncStatus == CloudSyncStatus.SyncRequested)
            {
                wrapper.SyncStatus = CloudSyncStatus.Syncing;
            }
        }

        public IEnumerable<string> GetClipboardItemIds(IEnumerable<ISearchTerm> searchTerms)
        {
            if (searchTerms != null && searchTerms.Any())
            {
                return _searchService
                    .FindClipboardItemIds(searchTerms)
                    // since the id begins with a timestamp of the item's creation,
                    // we sort the ids so that the newest items come on top
                    .OrderByDescending(id => id);
            }
            else
            {
                return base.ClipboardManager.GetAllItemIds();
            }
        }

        public void LoadAllItems(MonitorDimensions monitorDimensions, IEnumerable<string> clipboardItemIds)
        {
            base.LoadAndWrapItems(monitorDimensions, clipboardItemIds);
            LoadSyncStatusOfItems();
        }

        private void LoadSyncStatusOfItems()
        {
            foreach (var wrapper in ClipboardViewItemWrappers)
            {
                if (!(wrapper.ClipboardItem is ICloudSynchronizable cloudSyncItem))
                {
                    throw new InvalidOperationException($"Looks like the ClipboardItem does not implement {nameof(ICloudSynchronizable)}");
                }

                IHaveTransferabilityInfo transferabilityInfo = wrapper.ClipboardItem;

                if (!transferabilityInfo.CanTransfer)
                {
                    continue;
                }

                if (wrapper.IsItemTransferable != transferabilityInfo.CanTransfer)
                {
                    wrapper.IsItemTransferable = transferabilityInfo.CanTransfer;
                }

                if (!transferabilityInfo.ShouldTransfer)
                {
                    wrapper.SyncStatus = CloudSyncStatus.SyncDisabled;
                }
                else
                {
                    if (cloudSyncItem.IsSynchronizedWithCloud)
                    {
                        wrapper.SyncStatus = CloudSyncStatus.SyncComplete;
                    }
                    else if (IsCloudSyncAvailable)
                    {
                        wrapper.SyncStatus = CloudSyncStatus.Syncing;
                    }
                    else
                    {
                        wrapper.SyncStatus = CloudSyncStatus.SyncRequested;
                    }
                }
            }
        }

        public void DeleteItems(IEnumerable<string> clipboardItemIds)
        {
            // do not loop through the collection, passed as a method parameter here, 
            // as it may be the SelectedItems collection, which will change when we
            // remove the selected items and this will throw an exception...
            var itemsToRemove = base.ClipboardViewItemWrappers
                .Where(item => clipboardItemIds.Contains(item.Id))
                .ToList();
            
            foreach (var item in itemsToRemove)
            {
                // remove the item from the clipboard manager
                base.ClipboardManager.RemoveClipboardItem(item.Id);
                // remove the item from the collection, bound to the view; 
                base.ClipboardViewItemWrappers.Remove(item);
            }
        }

        protected override void Dispose(bool disposing)
        {
            // if we do not unsubscribe, our view model will remain alive for the lifetime of the sync service,
            // which is the same as the lifetime of the program.
            _cloudSyncService.AuthenticationStateChanged -= _cloudSyncService_AuthenticationStateChanged;
            _cloudSyncService.ItemSynced -= _cloudSyncService_ItemSynced;
            
            // without cleaning this collection, we get a memory leak
            // if the view is closed without the items are fully loaded.
            ClipboardViewItemWrappers.Clear();

            base.Dispose(disposing);
        }

        ~MainViewClipboardContentsViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
