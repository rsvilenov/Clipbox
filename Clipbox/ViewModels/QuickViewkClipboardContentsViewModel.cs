using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.ViewModels
{
    public class QuickViewkClipboardContentsViewModel : ClipboardContentsViewModelBase
    {
        public ICommand ClearClipboardEntries { get; set; }

        private List<ClipboardViewItemWrapper> _selectedClipboardViewItemWrappers;

        public List<ClipboardViewItemWrapper> SelectedClipboardViewItemWrappers
        {
            get { return _selectedClipboardViewItemWrappers; }
            set { SetField(ref _selectedClipboardViewItemWrappers, value); }
        }

        private Queue<string> _newlyAddedItemsToLoadAndWrap;

        private MonitorDimensions _lastUsedMonitorDimensions;

        private SlidingTimer _loadNewlyAddedItemsSlidingTimer;

        /// <summary>
        /// This delay is to reduce the occurance of the cases when many
        /// clipboard items has been added to the clipboard manager in a short time and each item
        /// blocks the thread of NewItemAdded handler while it loads its view item... With this delay,
        /// a bunch of newly added items within its duration will load their view items asynchronously
        /// after the NewItemAdded has fired...
        /// However, I am not sure how often these cases will happen and if I need this at all...
        /// </summary>
        private const int NEWLY_ADDED_ITEMS_LOAD_DELAY_MS = 200;

        public QuickViewkClipboardContentsViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService)
                : base(clipboardManager, previewGenerationService, settingsService)
        {
            ClearClipboardEntries = new DelegateCommand(ClearAllClipboardEntriesHandler);

            SelectedClipboardViewItemWrappers = new List<ClipboardViewItemWrapper>();
            _newlyAddedItemsToLoadAndWrap = new Queue<string>();

            _loadNewlyAddedItemsSlidingTimer = new SlidingTimer();
            _loadNewlyAddedItemsSlidingTimer.IntervalMs = NEWLY_ADDED_ITEMS_LOAD_DELAY_MS;
            _loadNewlyAddedItemsSlidingTimer.Elapsed += _loadNewlyAddedItemsSlidingTimer_Elapsed;

            ClipboardManager.ItemAdded += ClipboardManager_ItemAdded;
            ClipboardManager.ItemChanged += ClipboardManager_ItemChanged;
        }

        private void ClearAllClipboardEntriesHandler()
        {
            base.ClipboardManager.Clear();
            base.ClipboardManager.ClearSystemClipboard();
            base.ClipboardViewItemWrappers.Clear();
        }

        private void ClipboardManager_ItemChanged(object sender, ClipboardManagerEventArgs e)
        {
            if (RemoveItemWrapper(e.ItemId))
            {
                ScheduleLoadingOfNewItem(e.ItemId);
            }
        }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        private async void ClipboardManager_ItemAdded(object sender, ClipboardManagerEventArgs e)
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            ScheduleLoadingOfNewItem(e.ItemId);
        }

        public void ScheduleLoadingOfNewItem(string clipboardItemId)
        {
            _newlyAddedItemsToLoadAndWrap.Enqueue(clipboardItemId);

            if (_newlyAddedItemsToLoadAndWrap.Count > SettingsService.GetSetting<int>(SettingsKeys.MaxNonPinnedItemsInQuickView))
            {
                _newlyAddedItemsToLoadAndWrap.Dequeue();
            }

            _loadNewlyAddedItemsSlidingTimer.Slide();
        }

        private void _loadNewlyAddedItemsSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => LoadAndWrapNewlyAddedItems()));
        }

        private void LoadAndWrapNewlyAddedItems()
        {
            if (_lastUsedMonitorDimensions == null)
            {
                throw new InvalidCastException($"{nameof(_lastUsedMonitorDimensions)} is null. Call {nameof(LoadWrappedPinnedItems)} first.");
            }

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.Started;

            /* since we are using a queue, our newest items will be last, therefore we should reverse it */
            IEnumerable<string> newlyAddedItemsByEntryOrderReversed = _newlyAddedItemsToLoadAndWrap.Reverse();

            base.LoadAndWrapItems(_lastUsedMonitorDimensions,
               newlyAddedItemsByEntryOrderReversed,
               addAtBeginning: true);

            RemoveOldItemWrappersExceedingLimit();

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.WrappersLoaded;

            foreach (string clipboardItemId in newlyAddedItemsByEntryOrderReversed)
            {
                base.LoadViewItem(clipboardItemId, _lastUsedMonitorDimensions);
            }

            _newlyAddedItemsToLoadAndWrap.Clear();

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.ViewItemsLoaded;
        }

        private void RemoveOldItemWrappersExceedingLimit()
        {
            IEnumerable<string> unPinnedItemWrappers = base.ClipboardViewItemWrappers
                            .Where(w => !w.ClipboardItem.IsPinned)
                            .Select(w => w.Id);
            int nItemsToRemove = unPinnedItemWrappers.Count() - SettingsService.GetSetting<int>(SettingsKeys.MaxNonPinnedItemsInQuickView);
            if (nItemsToRemove > 0)
            {
                IEnumerable<string> itemsToRemove = unPinnedItemWrappers.Reverse().Take(nItemsToRemove);
                base.RemoveClipboardItemWrappers(itemsToRemove);
            }
        }

        public void LoadWrappedPinnedItems(MonitorDimensions monitorDimensions)
        {
            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.Started;

            var clipboardItemIds = base.ClipboardManager.GetAllItemIds();
            
            // load all pinned items
            IEnumerable<string> pinnedItems = base.ClipboardManager.GetAllItemIds((item) => item.IsPinned);
            base.ClipboardViewItemWrappers.Clear();
            base.LoadAndWrapItems(monitorDimensions, pinnedItems);

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.WrappersLoaded;

            _lastUsedMonitorDimensions = monitorDimensions;
        }

        public void LoadViewItems(MonitorDimensions monitorDimensions)
        {
            var clipboardItemIds = base.ClipboardViewItemWrappers.Select(w => w.Id).ToList();

            foreach (string clipboardItemId in clipboardItemIds)
            {
#if DEBUG
                Debug.WriteLine($"Loading view of item {clipboardItemId}");
#endif
                base.LoadViewItem(clipboardItemId, monitorDimensions);

            }

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.ViewItemsLoaded;
        }
    }
}
