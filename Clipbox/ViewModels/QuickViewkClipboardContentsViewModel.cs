using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
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

        public ICommand DeleteClipboardEntry { get; set; }

        public ICommand PinUnpinClipboardEntry { get; set; }
        
        private List<ClipboardViewItemWrapper> _selectedClipboardViewItemWrappers;

        public List<ClipboardViewItemWrapper> SelectedClipboardViewItemWrappers
        {
            get { return _selectedClipboardViewItemWrappers; }
            set { SetField(ref _selectedClipboardViewItemWrappers, value); }
        }
        
        private List<string> _newlyAddedItemsToLoadAndWrap;

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
            DeleteClipboardEntry = new DelegateCommand<ClipboardViewItem>(DeleteClipboardEntryHandler);

            // wait for the binding operations to complete,
            // so we can get the changed value of the IsPinned property
            PinUnpinClipboardEntry = new DelegateCommand<ClipboardViewItem>(PinUnpinClipboardEntryHandler,
                waitBackgroundOperationsFirst: true);
            
            SelectedClipboardViewItemWrappers = new List<ClipboardViewItemWrapper>();
            _newlyAddedItemsToLoadAndWrap = new List<string>();

            _loadNewlyAddedItemsSlidingTimer = new SlidingTimer();
            _loadNewlyAddedItemsSlidingTimer.IntervalMs = NEWLY_ADDED_ITEMS_LOAD_DELAY_MS;
            _loadNewlyAddedItemsSlidingTimer.Elapsed += _loadNewlyAddedItemsSlidingTimer_Elapsed;

            clipboardManager.ItemAdded += ClipboardManager_ItemAdded;
            clipboardManager.ItemRemoved += ClipboardManager_ItemRemoved;
            clipboardManager.ItemChanged += ClipboardManager_ItemChanged;
        }

        private void _loadNewlyAddedItemsSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            LoadAndWrapNewlyAddedItems();
        }

        private void PinUnpinClipboardEntryHandler(ClipboardViewItem obj)
        {
            base.ClipboardManager.PinUnpinItemItem(obj.ClipboardItemId, obj.IsPinned);
        }

        private void DeleteClipboardEntryHandler(ClipboardViewItem parameter)
        {
            base.ClipboardManager.RemoveClipboardItem(parameter.ClipboardItemId);
        }

        private void ClearAllClipboardEntriesHandler()
        {
            base.ClipboardManager.Clear();
            base.ClipboardManager.ClearSystemClipboard();
            base.ClipboardViewItemWrappers.Clear();
        }

        private void ClipboardManager_ItemChanged(object sender, ClipboardManagerEventArgs e)
        {
            if (RemoveItem(e.ItemId))
            {
                AddNewItem(e.ItemId);
            }
        }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        private async void ClipboardManager_ItemAdded(object sender, ClipboardManagerEventArgs e)
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            AddNewItem(e.ItemId);
        }

        private void ClipboardManager_ItemRemoved(object sender, ClipboardManagerEventArgs e)
        {
            RemoveItem(e.ItemId);
        }

        private bool RemoveItem(string clipboardItemId)
        {
            var itemToRemove = base.ClipboardViewItemWrappers.FirstOrDefault(item => item.Id == clipboardItemId);
            if (itemToRemove != null)
            {
                base.ClipboardViewItemWrappers.Remove(itemToRemove);
                return true;
            }

            return false;
        }

        private void AddNewItem(string clipboardItemId)
        {
            _newlyAddedItemsToLoadAndWrap.Add(clipboardItemId);
            _loadNewlyAddedItemsSlidingTimer.Slide();
        }

        private void LoadAndWrapNewlyAddedItems()
        {
            if (_lastUsedMonitorDimensions == null)
            {
                throw new InvalidCastException($"{nameof(_lastUsedMonitorDimensions)}is null. Call {nameof(ReloadWrappedItems)} first.");
            }

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.Started;

            base.LoadAndWrapItems(_lastUsedMonitorDimensions, _newlyAddedItemsToLoadAndWrap);

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.WrappersLoaded;

            foreach (string clipboardItemId in _newlyAddedItemsToLoadAndWrap)
            {
                base.LoadViewItem(clipboardItemId, _lastUsedMonitorDimensions);
            }

            _newlyAddedItemsToLoadAndWrap.Clear();

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.ViewItemsLoaded;
        }

        public void ReloadWrappedItems(MonitorDimensions monitorDimensions)
        {
            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.Started;

            var clipboardItemIds = base.ClipboardManager.GetAllItemIds();

            IEnumerable<string> allItemIds = base.ClipboardManager.GetAllItemIds();
            base.ClipboardViewItemWrappers.Clear();
            base.LoadAndWrapItems(monitorDimensions, allItemIds, pinnedOnly: false);

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.WrappersLoaded;

            _lastUsedMonitorDimensions = monitorDimensions;
        }

        public void LoadViewItems(MonitorDimensions monitorDimensions)
        {
            var clipboardItemIds = base.ClipboardViewItemWrappers.Select(w => w.Id).ToList();

            foreach(string clipboardItemId in clipboardItemIds)
            {
#if DEBUG
                Debug.WriteLine($"Loading view of item {clipboardItemId}");
#endif
                base.LoadViewItem(clipboardItemId, monitorDimensions);

            }

            ClipboardItemsLoadingStage = ClipboardItemsLoadingStage.ViewItemsLoaded;
        }

        public IClipboardItem GetClipboardItem(string id, bool restoreShrinked)
        {
            return base.ClipboardManager.GetClipboardItem(id, restoreShrinked);
        }


    }
}
