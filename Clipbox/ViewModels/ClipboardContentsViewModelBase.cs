using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.ViewModels
{
    public abstract class ClipboardContentsViewModelBase : BindableObject
    {
        private AsyncObservableCollection<ClipboardViewItemWrapper> _clipboardViewItemWrappers;

        public AsyncObservableCollection<ClipboardViewItemWrapper> ClipboardViewItemWrappers
        {
            get { return _clipboardViewItemWrappers; }
            set { SetField(ref _clipboardViewItemWrappers, value); }
        }

        private ClipboardItemsLoadingStage _clipboardItemLoadingStage;

        public ClipboardItemsLoadingStage ClipboardItemsLoadingStage
        {
            get { return _clipboardItemLoadingStage; }
            set { SetField(ref _clipboardItemLoadingStage, value); }
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
        }

        public ClipboardViewItemWrapper GetViewItemWrapper(string clipboardItemId)
        {
            var restoredClipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
            return _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
        }

        public ClipboardViewItem GetViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            var restoredClipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
            PreviewDataTransferObject pdto = PreviewGenerationService.GetPreviewDataTransferObject(restoredClipboardItem, monitorDimensions);
            return ClipboardViewItemFactory.CreateFromPreviewDataTransferObject(restoredClipboardItem, pdto);
        }
        
        [Obsolete("Use GetViewItemWrapper, GetViewItem and TryGetPreviewDimensions from within the view " +
            "and use the dispatcher background priority for assigning the bindable properties to the wrapper" +
            ", as running all three in a row synchronously and not in the dispatcher backgroud thread causes the UI to update painfully slow.")]
        public void LoadViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            ClipboardViewItemWrapper wrapper = GetViewItemWrapper(clipboardItemId);
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

        public void LoadAndWrapItems(MonitorDimensions monitorDimensions, IEnumerable<string> clipboardItemIds, bool pinnedOnly = false)
        {
            List<IClipboardItem> clipboardItems = new List<IClipboardItem>();
            foreach (string clipboardItemId in clipboardItemIds)
            {
                IClipboardItem clipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId);
                clipboardItems.Add(clipboardItem);
                if (pinnedOnly && !clipboardItem.IsPinned)
                {
                    continue;
                }

                var wrapper = new ClipboardViewItemWrapper(clipboardItem);

                if (TryGetPreviewDimensions(monitorDimensions, wrapper, out ClipboardViewItemDimensionInfo cvItemDimInfo))
                {
                    wrapper.DimensionInfo = cvItemDimInfo;
                }

                ClipboardViewItemWrappers.Add(wrapper);
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
            var wrappersToRemove = ClipboardViewItemWrappers
                .Where(w => clipboardItemIds.Contains(w.Id))
                .ToList();

            foreach (var wrapper in wrappersToRemove)
            {
                ClipboardViewItemWrappers.Remove(wrapper);
            }
        }

    }
}
