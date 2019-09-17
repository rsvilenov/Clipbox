using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ViewModels
{
    public class MainViewClipboardContentsViewModel : ClipboardContentsViewModelBase
    {
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
        
        private CancellableTaskManager _cancellableTaskManager;

        public MainViewClipboardContentsViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService) 
                : base(clipboardManager, previewGenerationService, settingsService)
        {
            _cancellableTaskManager = new CancellableTaskManager();
        }

        public void ScheduleLoadingOfViewItem(string clipboardItemId, MonitorDimensions monitorDimensions)
        {
            _cancellableTaskManager.ScheduleTask(clipboardItemId, new Action<string>((taskId) =>
            {
                base.LoadViewItem(clipboardItemId, monitorDimensions);
            }));
        }

        public void CancelLoadingOfViewItem(string clipboardItemId)
        {
            _cancellableTaskManager.CancelScheduledTask(taskId: clipboardItemId);
        }

        public IEnumerable<string> GetClipboardItemIds()
        {
            return base.ClipboardManager.GetAllItemIds();
        }
        
        public void LoadAllItems(MonitorDimensions monitorDimensions, IEnumerable<string> clipboardItemIds)
        {
            base.LoadAndWrapItems(monitorDimensions, clipboardItemIds);
        }

        public void DeleteItems(IEnumerable<string> clipboardItemIds)
        {
            foreach (string clipboardItemId in clipboardItemIds)
            {
                // remove the item from the clipboard manager
                base.ClipboardManager.RemoveClipboardItem(clipboardItemId);
            }

            // remove the item from the collection, bound to the view; 
            // do not modify the collection, passed as a method parameter here, 
            // as it may be the SelectedItems collection, which will change when we
            // remove the selected items and this will throw an exception...
            var itemsToRemove = base.ClipboardViewItemWrappers
                .Where(item => clipboardItemIds.Contains(item.Id))
                .ToList();
            foreach (var item in itemsToRemove)
            {
                base.ClipboardViewItemWrappers.Remove(item);
            }
        }

        ~MainViewClipboardContentsViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
