using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
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
        private int _previewItemWidth;
        private int _previewItemHeight;

        private CancellableTaskManager _cancellableTaskManager;

        public MainClipboardViewViewModel(IClipboardManager clipboardManager, IClipboardFormatPreviewExtentionContainer previewsContainer, ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;

            int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);
            _clipboardViewItemFactory = new ClipboardViewItemFactory(previewsContainer, maxPreviewItemsInGroupItem);
            _cancellableTaskManager = new CancellableTaskManager();
        }

        public void LoadViewItem(string clipboardItemId)
        {
            LazyLoadPreviewDimentions();
            LazyLoadClipboardViewItemFactory();

            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                var restoredClipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
                var wrapper = _clipboardViewItemWrappers.First(w => w.Id == clipboardItemId);
                wrapper.ViewItem = _clipboardViewItemFactory.Create(restoredClipboardItem, _previewItemWidth, _previewItemHeight);
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

        private void LazyLoadClipboardViewItemFactory()
        {
            if (_clipboardViewItemFactory == null)
            {
                _clipboardViewItemFactory = new ClipboardViewItemFactory(_previewsContainer);
            }
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
            foreach (string clipboardItemId in allItemIds)
            {
                IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId);
                var tempViewItem = new ClipboardViewItemWrapper(clipboardItem);
                ClipboardViewItemWrappers.Add(tempViewItem);
            }

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
