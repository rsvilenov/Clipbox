using Clipbox.Core.ClipboardContent;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.PreviewRelatedObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IClipboardFormatPreviewExtentionContainer _previewsContainer;

        private ClipboardViewItemFactory _clipboardViewItemFactory;
        private ClipboardViewItemManager _previewDataManager;
        private int _previewItemWidth;
        private int _previewItemHeight;

        private CancellableTaskManager _cancellableTaskManager;

        public MainClipboardViewViewModel(IClipboardManager clipboardManager,
            IClipboardFormatPreviewExtentionContainer previewsContainer,
            ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;
            _previewsContainer = previewsContainer;

            int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);
            _clipboardViewItemFactory = new ClipboardViewItemFactory(previewsContainer, maxPreviewItemsInGroupItem);
            _previewDataManager = new ClipboardViewItemManager(_clipboardViewItemFactory);
            _cancellableTaskManager = new CancellableTaskManager();
        }

        public void LoadViewItem(string clipboardItemId, PreviewDimensionSuggestedInfo previewDimensionSuggestedInfo)
        {
            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                var restoredClipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
                var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
                wrapper.ViewItem = _previewDataManager.GetClipboardViewItem(restoredClipboardItem,
                    previewDimensionSuggestedInfo.SuggestedPreviewWidth,
                    previewDimensionSuggestedInfo.SuggestedPreviewHeight); //_clipboardViewItemFactory.CreateFromClipboardItem(restoredClipboardItem, _previewItemWidth, _previewItemHeight);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                wrapper.IsViewItemLoaded = true;
                
                if (wrapper.ClipboardItem.PreviewDimensionInfo == null)
                {
                    StorePreviewDimensionsInfo(previewDimensionSuggestedInfo, wrapper);
                }
            }));
        }

        private void StorePreviewDimensionsInfo(PreviewDimensionSuggestedInfo previewDimensionSuggestedInfo, ClipboardViewItemWrapper wrapper)
        {
            PreviewDimensionInfo pdi = null;

            if (wrapper.ViewItem is ClipboardViewItemGroup)
            {
                // get all but the NMore items
                var actualViewItemsInGroup = ((ClipboardViewItemGroup)wrapper.ViewItem).PreviewItems.Where(vi => vi is ClipboardViewSubItem).Cast<ClipboardViewSubItem>();

                pdi = new PreviewDimensionInfo
                {
                    IsViewItemGroup = true,
                    PreviewDimentions = actualViewItemsInGroup.Select(subItem => new PreviewDimensions
                    {
                        PreviewHeight = subItem.PreviewItem?.Image?.PixelHeight ?? 0,
                        PreviewWidth = subItem.PreviewItem?.Image?.PixelWidth ?? 0,
                    }),
                    DPI = previewDimensionSuggestedInfo.MonitorDimensions.MonitorDPI,
                    WorkAreaHeight = previewDimensionSuggestedInfo.MonitorDimensions.MonitorHeight,
                    WorkAreaWidth = previewDimensionSuggestedInfo.MonitorDimensions.MonitorWidth
                };

            }
            else if (wrapper.ViewItem is ClipboardViewItemSingle)
            {
                var previewImg = ((ClipboardViewItemSingle)wrapper.ViewItem).PreviewItem?.Image;
                pdi = new PreviewDimensionInfo
                {
                    IsViewItemGroup = false,
                    PreviewDimentions = new List<IPreviewDimensions>
                        {
                            new PreviewDimensions
                            {
                                PreviewHeight = previewImg?.PixelHeight ?? 0,
                                PreviewWidth = previewImg?.PixelWidth ?? 0
                            }
                        },
                    DPI = previewDimensionSuggestedInfo.MonitorDimensions.MonitorDPI,
                    WorkAreaHeight = previewDimensionSuggestedInfo.MonitorDimensions.MonitorHeight,
                    WorkAreaWidth = previewDimensionSuggestedInfo.MonitorDimensions.MonitorWidth
                };
            }

            if (pdi != null)
            {
                _clipboardManager.SetPreviewDimensions(wrapper.Id, pdi, beSilent: true);
            }
        }

        public void LoadViewItem(string clipboardItemId)
        {
            LazyLoadPreviewDimentions();

            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                var restoredClipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
                var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
                wrapper.ViewItem = _previewDataManager.GetClipboardViewItem(restoredClipboardItem, _previewItemWidth, _previewItemHeight); //_clipboardViewItemFactory.CreateFromClipboardItem(restoredClipboardItem, _previewItemWidth, _previewItemHeight);

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

        private void LazyLoadPreviewDimentions()
        {
            if (_previewItemHeight == 0)
            {
                var previewDimArgs = new RequestPreviewDimentionsEventArgs();
                OnPreviewDimentionsRequested?.Invoke(this, previewDimArgs);
                _previewItemHeight = previewDimArgs.PreviewHeight;
                _previewItemWidth = previewDimArgs.PreviewWidth;
            }
        }

        public void LoadItems()
        {
            //const int NUMBER_OF_ITEMS = 1000;

            ClipboardViewItemWrappers = new AsyncObservableCollection<ClipboardViewItemWrapper>();
            IEnumerable<string> allItemIds = _clipboardManager.GetAllItemIds();
            List<IClipboardItem> clipboardItems = new List<IClipboardItem>();
            foreach (string clipboardItemId in allItemIds)
            {
                IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId);
                clipboardItems.Add(clipboardItem);
                var tempViewItem = new ClipboardViewItemWrapper(clipboardItem);
                ClipboardViewItemWrappers.Add(tempViewItem);
            }

            // TODO: this should be fast enough, but I should nevertheless consider doing this on closing the window
            Task.Run(() =>
            {
                _previewDataManager.DeleteUnusedStoredPreviewData(clipboardItems);
            });

            //int currentItemForTheDay = 0;
            //int currentNumber = NUMBER_OF_ITEMS;
            //for (int i = 0; i < NUMBER_OF_ITEMS; i++)
            //{
            //    Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            //    {
            //        int itemsPerDay = new Random().Next(1, 30);
            //        // by the time the dispatcher executes this code, the counter 
            //        // in the loop has already reached its maximum value, so we cannot
            //        // count on "i" for getting the number of the element.
            //        DateTime date = DateTime.Today.AddDays(currentNumber);
            //        if (currentItemForTheDay >= itemsPerDay)
            //        {

            //            currentNumber--;
            //            currentItemForTheDay = 0;
            //        }

            //        ClipboardViewItems.Add(new MainClipboardViewItem
            //        {
            //            DateCreated = date
            //        });
            //        currentItemForTheDay++;
            //    }));

            //}
        }

        ~MainClipboardViewViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
