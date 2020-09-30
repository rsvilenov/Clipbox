using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Core.Search;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ViewModels
{
    public class MainViewClipboardContentsViewModel : ClipboardContentsViewModelBase
    {
        private readonly IClipboardManager _clipboardManager;
        private readonly ISettingsService _settingsService;
        private readonly ISearchService _searchService;
        private readonly ICloudSynchronizationService _cloudSyncService;

        public event EventHandler RequestCloseOfMainWindow;


        public MainViewClipboardContentsViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService,
            ISearchService searchService,
            ICloudSynchronizationService cloudSyncService) 
                : base(clipboardManager, previewGenerationService, settingsService)
        {
            _settingsService = settingsService;
            _searchService = searchService;
            _cloudSyncService = cloudSyncService;
            _clipboardManager = clipboardManager;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
            _cloudSyncService.AuthenticationStateChanged += _cloudSyncService_AuthenticationStateChanged;
            _cloudSyncService.ItemSynced += _cloudSyncService_ItemSynced;
            _cloudSyncService.ErrorStateChanged += _cloudSyncService_ErrorStateChanged;

            _clipboardManager.ItemAdded += ClipboardManager_ItemAdded;

            IsCloudSyncAvailable = CheckIsCloudServiceAvailable();
            IsCloudSyncSvcInErrorState = _cloudSyncService.IsEnabled
                && _cloudSyncService.IsCloudSyncEnabled
                && _cloudSyncService.IsInErrorState;
            IsCloudSyncServiceEnabled = _cloudSyncService.IsEnabled;

            ClipboardItemsDataProvider = clipboardManager;

            IsSearchEnabled = settingsService.GetSetting<bool>(SettingsKeys.IsSearchEnabled);

            try
            {
                IsSearchIndexHealthy = _searchService.AreIndicesHealthy;
            }
            catch (Exception ex)
            {
                // TODO: log non fatal exception
                IsSearchIndexHealthy = false;
            }
        }

        private void _cloudSyncService_ErrorStateChanged(object sender, ErrorStateEventArgs e)
        {
            IsCloudSyncSvcInErrorState = _cloudSyncService.IsEnabled
                && _cloudSyncService.IsCloudSyncEnabled
                && e.State == Services.Enums.ErrorState.Error;
        }

        private void ClipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ItemAddedEventArgs e)
        {
            if (e.Source == ExtendedClipboard.Enums.ItemSource.SystemClipboard)
            {
                RequestCloseOfMainWindow?.Invoke(this, new EventArgs());
            }
        }

        private void _settingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.IsSearchEnabled)
            {
                IsSearchEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsSearchEnabled);
            }
            else if (e.Key == SettingsKeys.IsCloudSyncEnabled)
            {
                if (e.Value is bool enabled && enabled)
                {
                    IsCloudSyncAvailable = CheckIsCloudServiceAvailable();
                }
                else
                {
                    IsCloudSyncAvailable = false;
                }
            }
            else if (e.Key == SettingsKeys.IsCloudServicesEnabled)
            {
                IsCloudSyncServiceEnabled = e.Value is bool enabled && enabled;
            }
        }

        private void _cloudSyncService_ItemSynced(object sender, Services.EventArgsClasses.SyncEventArgs e)
        {
            var wrapper = ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == e.ClipboardItemId);
            if (wrapper != null)
            {
                wrapper.SyncStatus = CloudSyncStatus.SyncComplete;
            }
        }

        private void _cloudSyncService_AuthenticationStateChanged(object sender, AuthenticationStageEventArgs e)
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

            IsCloudSyncSvcInErrorState = _cloudSyncService.IsEnabled
                && _cloudSyncService.IsCloudSyncEnabled
                && e.Stage == Core.Extensibility.Enums.AuthenticationStage.AuthenticationError;
        }

        private bool CheckIsCloudServiceAvailable()
        {
            return _cloudSyncService.IsEnabled
                && _cloudSyncService.IsCloudSyncEnabled
                && _cloudSyncService.IsNetworkAvailable 
                && _cloudSyncService.IsAuthenticated;
        }

        private bool _isCloudSyncAvailable;
        public bool IsCloudSyncAvailable
        {
            get { return _isCloudSyncAvailable; }
            set { SetField(ref _isCloudSyncAvailable, value); }
        }

        private bool _isCloudSyncSvcInErrorState;
        public bool IsCloudSyncSvcInErrorState
        {
            get { return _isCloudSyncSvcInErrorState; }
            set { SetField(ref _isCloudSyncSvcInErrorState, value); }
        }

        private bool _isCloudSyncServiceEnabled;
        public bool IsCloudSyncServiceEnabled
        {
            get { return _isCloudSyncServiceEnabled; }
            set { SetField(ref _isCloudSyncServiceEnabled, value); }
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

        private bool _isSearchIndexHealthy;
        public bool IsSearchIndexHealthy
        {
            get { return _isSearchIndexHealthy; }
            set { SetField(ref _isSearchIndexHealthy, value); }
        }

        private bool _isSearchEnabled;
        public bool IsSearchEnabled
        {
            get { return _isSearchEnabled; }
            set { SetField(ref _isSearchEnabled, value); }
        }

        private IClipboardItemsDataProvider _clipboardItemsDataProvider;
        public IClipboardItemsDataProvider ClipboardItemsDataProvider
        {
            get { return _clipboardItemsDataProvider; }
            set { SetField(ref _clipboardItemsDataProvider, value); }
        }

        public IEnumerable<string> GetClipboardItemIds(IEnumerable<ISearchTerm> searchTerms)
        {
            if (!IsSearchIndexHealthy && IsSearchEnabled)
            {
                return Enumerable.Empty<string>();
            }

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
            _cloudSyncService.ErrorStateChanged -= _cloudSyncService_ErrorStateChanged;

            _clipboardManager.ItemAdded -= ClipboardManager_ItemAdded;

            _settingsService.SettingChanged -= _settingsService_SettingChanged;
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
