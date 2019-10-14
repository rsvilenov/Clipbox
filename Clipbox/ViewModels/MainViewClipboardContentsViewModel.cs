using Clipbox.Core.Search;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels.Enums;
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
            IsCloudSyncAvailable = CheckIsCloudServiceAvailable();

            _cloudSyncService.ItemSynced += _cloudSyncService_ItemSynced;
        }
        
        private void _cloudSyncService_ItemSynced(object sender, Services.EventArgsClasses.SyncEventArgs e)
        {
            var wrapper = ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == e.ClipboardItemId);
            if (wrapper != null && wrapper.IsViewItemLoaded && wrapper.ViewItem != null)
            {
                wrapper.ViewItem.SyncStatus = CloudSyncStatus.SyncComplete;
            }
        }

        private void _cloudSyncService_AuthenticationStateChanged(object sender, System.EventArgs e)
        {
            IsCloudSyncAvailable = CheckIsCloudServiceAvailable();
        }

        private bool CheckIsCloudServiceAvailable()
        {
            return _cloudSyncService.IsNetworkAvailable && _cloudSyncService.IsAuthenticated;
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
            if (wrapper != null && wrapper.IsViewItemLoaded && wrapper.ViewItem != null)
            {
                wrapper.ViewItem.SyncStatus = IsCloudSyncAvailable 
                    ? CloudSyncStatus.Syncing : CloudSyncStatus.SyncRequested;
            }
        }

        public IEnumerable<string> GetClipboardItemIds(IEnumerable<ISearchTerm> searchTerms)
        {
            if (searchTerms != null && searchTerms.Any())
            {
                return _searchService.FindClipboardItemIds(searchTerms);
            }
            else
            {
                return base.ClipboardManager.GetAllItemIds();
            }
        }
        
        public void LoadAllItems(MonitorDimensions monitorDimensions, IEnumerable<string> clipboardItemIds)
        {
            base.LoadAndWrapItems(monitorDimensions, clipboardItemIds);
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
            _cloudSyncService.AuthenticationStateChanged -= _cloudSyncService_AuthenticationStateChanged;

            base.Dispose(disposing);
        }

        ~MainViewClipboardContentsViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
