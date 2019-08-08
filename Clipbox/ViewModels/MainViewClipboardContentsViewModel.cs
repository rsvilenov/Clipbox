using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        

        public void LoadAllItems(MonitorDimensions monitorDimensions)
        {
            IEnumerable<string> allItemIds = base.ClipboardManager.GetAllItemIds();
            base.LoadAndWrapItems(monitorDimensions, allItemIds);
        }

        ~MainViewClipboardContentsViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
