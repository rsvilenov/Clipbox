using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ViewModels
{
    public class MainClipboardViewViewModel : BindableObject
    {
        public event EventHandler<RequestPreviewDimentionsEventArgs> OnPreviewDimentionsRequested;

        private AsyncObservableCollection<ClipboardViewItemWrapper> _clipboardViewItemWrappers;

        public AsyncObservableCollection<ClipboardViewItemWrapper> ClipboardViewItemWrappers
        {
            get { return _clipboardViewItemWrappers; }
            set { SetField(ref _clipboardViewItemWrappers, value); }
        }


        private string _currentVisibleItemsDateString;

        public string CurrentVisibleItemsDateString
        {
            get { return _currentVisibleItemsDateString; }
            set { SetField(ref _currentVisibleItemsDateString, value); }
        }

        private string _firstItemDateString;

        public string FirstItemDateString
        {
            get { return _firstItemDateString; }
            set { SetField(ref _firstItemDateString, value); }
        }

        private string _lastItemDateString;

        public string LastItemDateString
        {
            get { return _lastItemDateString; }
            set { SetField(ref _lastItemDateString, value); }
        }

        private readonly IClipboardManager _clipboardManager;
        private readonly ISettingsService _settingsService;
        private readonly IPreviewGenerationService _previewGenerationService;

        private CancellableTaskManager _cancellableTaskManager;

        public MainClipboardViewViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;
            _previewGenerationService = previewGenerationService;
            
            _cancellableTaskManager = new CancellableTaskManager();
        }

        public void LoadViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                var restoredClipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
                var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
                PreviewDataTransferObject pdto = _previewGenerationService.GetPreviewDataTransferObject(restoredClipboardItem, monitorDimensions);
                wrapper.ViewItem = ClipboardViewItemFactory.CreateFromPreviewDataTransferObject(restoredClipboardItem, pdto);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                wrapper.IsViewItemLoaded = true;
            }));
        }

        public void CancelLoadingOfViewItem(string clipboardItemId)
        {
            _cancellableTaskManager.CancelScheduledTask(taskId: clipboardItemId);
        }

        public void UnloadViewItem(string clipboardItemId)
        {
            var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
            wrapper.ViewItem = null;
            wrapper.IsViewItemLoaded = false;
        }

        public void LoadItems(MonitorDimensions monitorDimensions)
        {
            ClipboardViewItemWrappers = new AsyncObservableCollection<ClipboardViewItemWrapper>();
            IEnumerable<string> allItemIds = _clipboardManager.GetAllItemIds();
            List<IClipboardItem> clipboardItems = new List<IClipboardItem>();
            foreach (string clipboardItemId in allItemIds)
            {
                IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId);
                clipboardItems.Add(clipboardItem);
                var tempViewItem = new ClipboardViewItemWrapper(clipboardItem);

                CalibratedPreviewDimentionInfo cpdi = null;
                if (_previewGenerationService.TryGetQuickPreviewDimentionInfo(clipboardItemId, monitorDimensions, out cpdi))
                {
                    IEnumerable<ClipboardViewItemDimensions> dimensions = cpdi.PreviewDimentions
                        .Select(pd => 
                            new ClipboardViewItemDimensions
                            {
                                PreviewHeight = pd.PreviewHeight,
                                PreviewWidth = pd.PreviewWidth
                            });

                    tempViewItem.DimensionInfo = new ClipboardViewItemDimensionInfo
                    {
                        IsViewItemGroup = cpdi.IsViewItemGroup,
                        Dimensions = new AsyncObservableCollection<ClipboardViewItemDimensions>(dimensions)
                    };
                }
                
                ClipboardViewItemWrappers.Add(tempViewItem);
            }
        }

        ~MainClipboardViewViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
