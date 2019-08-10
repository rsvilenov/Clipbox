using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public void LoadViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            var restoredClipboardItem = ClipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
            var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
            PreviewDataTransferObject pdto = PreviewGenerationService.GetPreviewDataTransferObject(restoredClipboardItem, monitorDimensions);
            wrapper.ViewItem = ClipboardViewItemFactory.CreateFromPreviewDataTransferObject(restoredClipboardItem, pdto);

#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

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

                var tempViewItem = new ClipboardViewItemWrapper(clipboardItem);

                CalibratedPreviewDimentionInfo cpdi = null;
                if (PreviewGenerationService.TryGetQuickPreviewDimentionInfo(clipboardItemId, monitorDimensions, out cpdi))
                {
                    IEnumerable<ClipboardViewSubItemDimension> dimensions = cpdi.PreviewDimentions
                        .Select(pd =>
                            new ClipboardViewSubItemDimension
                            {
                                PreviewHeight = pd.PreviewHeight,
                                PreviewWidth = pd.PreviewWidth
                            });

                    tempViewItem.DimensionInfo = new ClipboardViewItemDimensionInfo
                    {
                        IsViewItemGroup = cpdi.IsViewItemGroup,
                        Dimensions = new AsyncObservableCollection<ClipboardViewSubItemDimension>(dimensions),
                        NumberOfItemsWithoutPreview = cpdi.NumberOfItemsWithoutPreview
                    };
                }

                ClipboardViewItemWrappers.Add(tempViewItem);
            }
        }
    }
}
