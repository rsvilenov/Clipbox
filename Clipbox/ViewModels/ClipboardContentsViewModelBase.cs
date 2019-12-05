using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.ViewModels
{
    public abstract class ClipboardContentsViewModelBase : BindableObject, IDisposable
    {
        private AsyncObservableCollection<ClipboardViewItemWrapper> _clipboardViewItemWrappers;

        public AsyncObservableCollection<ClipboardViewItemWrapper> ClipboardViewItemWrappers
        {
            get { return _clipboardViewItemWrappers; }
            set { SetField(ref _clipboardViewItemWrappers, value); }
        }
        
        protected IClipboardManager ClipboardManager { get; private set; }
        protected ISettingsService SettingsService { get; private set; }
        protected IPreviewGenerationService PreviewGenerationService { get; private set; }
        

        public ClipboardContentsViewModelBase(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService)
        {
            ClipboardManager = clipboardManager;
            SettingsService = settingsService;
            PreviewGenerationService = previewGenerationService;

            ClipboardViewItemWrappers = new AsyncObservableCollection<ClipboardViewItemWrapper>();

            ClipboardManager.ItemRemoved += ClipboardManager_ItemRemoved;
        }

        private void ClipboardManager_ItemRemoved(object sender, ClipboardManagerEventArgs e)
        {
            RemoveItemWrapper(e.ItemId);
        }

        public ClipboardViewItemWrapper GetViewItemWrapper(string clipboardItemId)
        {
            return _clipboardViewItemWrappers.FirstOrDefault(w => w.Id == clipboardItemId);
        }

        public ClipboardViewItem GetViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            ClipboardViewItemWrapper wrapper = GetViewItemWrapper(clipboardItemId);
            PreviewDataTransferObject pdto = PreviewGenerationService.GetPreviewDataTransferObject(wrapper.ClipboardItem, monitorDimensions);
            return ClipboardViewItemFactory.CreateFromPreviewDataTransferObject(wrapper.ClipboardItem, pdto);
        }
        
        [Obsolete("Use GetViewItemWrapper, GetViewItem and TryGetPreviewDimensions from within the view " +
            "and use the dispatcher background priority for assigning the bindable properties to the wrapper" +
            ", as running all three in a row synchronously and not in the dispatcher backgroud thread causes the UI to update painfully slow.")]
        public void LoadViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            ClipboardViewItemWrapper wrapper = GetViewItemWrapper(clipboardItemId);
            if (wrapper == null)
            {
                throw new InvalidOperationException($"The wrapper of {clipboardItemId} cannot be found. It could have been filtered off, but it should not have been.");
            }

            wrapper.ViewItem = GetViewItem(clipboardItemId, monitorDimensions);

            if (TryGetPreviewDimensions(monitorDimensions, wrapper, out ClipboardViewItemDimensionInfo cvItemDimInfo))
            {
                wrapper.DimensionInfo = cvItemDimInfo;
            }

            wrapper.IsViewItemLoaded = true;
        }

        public void UnloadViewItem(string clipboardItemId)
        {
            var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
            wrapper.ViewItem = null;
            wrapper.IsViewItemLoaded = false;
        }

        public void LoadAndWrapItems(MonitorDimensions monitorDimensions,
            IEnumerable<string> clipboardItemIds,
            bool addAtBeginning = false,
            bool orderByDateDesc = true)
        {
            List<IClipboardItem> clipboardItems = new List<IClipboardItem>();
            foreach (string clipboardItemId in clipboardItemIds)
            {
                IClipboardItem clipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId);
                clipboardItems.Add(clipboardItem);
            }

            if (orderByDateDesc)
            {
                clipboardItems = clipboardItems
                    .OrderByDescending(item => item.TimestampInfo.EntryDateUtcTicks)
                    .ToList();
            }

            List<ClipboardViewItemWrapper> wrappers = new List<ClipboardViewItemWrapper>();
            foreach (IClipboardItem clipboardItem in clipboardItems)
            {
                var wrapper = new ClipboardViewItemWrapper(clipboardItem);

                if (TryGetPreviewDimensions(monitorDimensions, wrapper, out ClipboardViewItemDimensionInfo cvItemDimInfo))
                {
                    wrapper.DimensionInfo = cvItemDimInfo;
                }

                wrappers.Add(wrapper);
            }
            
            if (addAtBeginning)
            {
                ClipboardViewItemWrappers.InsertRange(0, wrappers);
            }
            else
            {
                ClipboardViewItemWrappers.AddRange(wrappers);
            }
        }
        
        public bool TryGetPreviewDimensions(MonitorDimensions monitorDimensions, 
            ClipboardViewItemWrapper wrapper,
            out ClipboardViewItemDimensionInfo cvItemDimInfo)
        {
            cvItemDimInfo = null;

            if (PreviewGenerationService.TryGetQuickPreviewDimentionInfo(wrapper.Id, monitorDimensions, out CalibratedPreviewDimentionInfo cpdi))
            {
                IEnumerable<ClipboardViewSubItemDimension> dimensions = cpdi.PreviewDimentions
                    .Select(pd =>
                        new ClipboardViewSubItemDimension
                        {
                            PreviewHeight = pd.PreviewHeight,
                            PreviewWidth = pd.PreviewWidth
                        });

                cvItemDimInfo = new ClipboardViewItemDimensionInfo
                {
                    IsViewItemGroup = cpdi.IsViewItemGroup,
                    Dimensions = new AsyncObservableCollection<ClipboardViewSubItemDimension>(dimensions),
                    NumberOfItemsWithoutPreview = cpdi.NumberOfItemsWithoutPreview
                };

                return true;
            }

            return false;
        }

        public void RemoveClipboardItemWrappers(IEnumerable<string> clipboardItemIds)
        {
            foreach (var id in clipboardItemIds
                .ToList() /* materialize the IEnumerable in any case,
                as it may carry an enumerator on the same collection that
                we are going to modify in this loop */ )
            {
                RemoveItemWrapper(id);
            }
        }

        protected virtual void OnSyncOfClipboardEntryEnabledOrDisabled(string clipboardItemId)
        {
        }

        public void EnbleDisableSyncOfClipboardEntry(string clipboardItemId)
        {
            ClipboardViewItemWrapper wrapper = ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == clipboardItemId);
            if (wrapper == null)
            {
                throw new KeyNotFoundException($"No clipboard item with id {clipboardItemId} was found.");
            }

            if (wrapper.SyncStatus == CloudSyncStatus.SyncDisabled)
            {
                wrapper.SyncStatus = CloudSyncStatus.SyncRequested;
            }
            else
            {
                wrapper.SyncStatus = CloudSyncStatus.SyncDisabled;
            }

            ClipboardManager.EnableDisableSyncOfItem(clipboardItemId, wrapper.SyncStatus != CloudSyncStatus.SyncDisabled);
            OnSyncOfClipboardEntryEnabledOrDisabled(clipboardItemId);
        }

        protected virtual void OnClipboardEntryPinnedUnpinned(string clipboardItemId)
        {
        }

        public void PinUnpinClipboardEntry(string clipboardItemId)
        {
            ClipboardViewItemWrapper wrapper = ClipboardViewItemWrappers.FirstOrDefault(w => w.Id == clipboardItemId);
            if (wrapper == null)
            {
                throw new KeyNotFoundException($"No clipboard item with id {clipboardItemId} was found.");
            }

            if (!wrapper.IsViewItemLoaded)
            {
                throw new InvalidOperationException($"The view item for {clipboardItemId} is not loaded. Please load it first.");
            }

            ClipboardManager.PinUnpinItemItem(clipboardItemId, wrapper.ViewItem.IsPinned);

            if (wrapper.ViewItem.IsPinned
                && SettingsService.GetSetting<bool>(SettingsKeys.IsAutomaticCloudSyncOfPinnedItemsEnabled))
            {
                ClipboardManager.EnableDisableSyncOfItem(clipboardItemId, true);
            }

            OnClipboardEntryPinnedUnpinned(clipboardItemId);
        }

        public void DeleteClipboardEntry(string clipboardItemId)
        {
            ClipboardManager.RemoveClipboardItem(clipboardItemId);
        }

        protected virtual void OnClipboardEntryWrapperRemoved(string clipboardItemId)
        {
        }

        protected bool RemoveItemWrapper(string clipboardItemId)
        {
            bool success = false;
            var itemToRemove = ClipboardViewItemWrappers.SingleOrDefault(item => item.Id == clipboardItemId);
            if (itemToRemove != null)
            {
                ClipboardViewItemWrappers.Remove(itemToRemove);
                success = true;
                OnClipboardEntryWrapperRemoved(itemToRemove.Id);
            }

            return success;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                ClipboardManager.ItemRemoved -= ClipboardManager_ItemRemoved;
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
