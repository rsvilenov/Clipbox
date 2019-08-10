using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Clipbox.ViewModels
{
    public class QuicViewkClipboardContentsViewModel : ClipboardContentsViewModelBase
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

        private bool _areClipboardItemsAvailable;

        public bool AreClipboardItemsAvailable
        {
            get { return _areClipboardItemsAvailable; }
            set { SetField(ref _areClipboardItemsAvailable, value); }
        }

        private List<string> _newlyAddedItemsToLoadAndWrap;

        //private double _itemDesiredHeight;

        //public double ItemDesiredHeight
        //{
        //    get { return _itemDesiredHeight; }
        //    set
        //    {
        //        if (_itemDesiredHeight != value)
        //        {
        //            Debug.WriteLine("ItemHeight changed to: " + value);
        //            ReloadItems((int)value, (int)value);
        //            SetField(ref _itemDesiredHeight, value);
        //        }
        //    }
        //}

        public QuicViewkClipboardContentsViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService)
                : base(clipboardManager, previewGenerationService, settingsService)
        {
            ClearClipboardEntries = new DelegateCommand(ClearAllClipboardEntriesHandler);
            DeleteClipboardEntry = new DelegateCommand<ClipboardViewItem>(DeleteClipboardEntryHandler);
            PinUnpinClipboardEntry = new DelegateCommand<ClipboardViewItem>(PinUnpinClipboardEntryHandler);
            
            SelectedClipboardViewItemWrappers = new List<ClipboardViewItemWrapper>();
            _newlyAddedItemsToLoadAndWrap = new List<string>();

            clipboardManager.ItemAdded += ClipboardManager_ItemAdded;
            clipboardManager.ItemRemoved += ClipboardManager_ItemRemoved;
            clipboardManager.ItemChanged += ClipboardManager_ItemChanged;
        }
        
        private void PinUnpinClipboardEntryHandler(ClipboardViewItem obj)
        {
            // wait for the binding operations to complete,
            // so we can get the changed value of the IsPinned property
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => 
            {
                base.ClipboardManager.PinUnpinItemItem(obj.ClipboardItemId, obj.IsPinned);
            }));
            
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
            AreClipboardItemsAvailable = false;
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

            AreClipboardItemsAvailable = true;
        }

        public void LoadAndWrapNewlyAddedItems(MonitorDimensions monitorDimensions)
        {
            base.LoadAndWrapItems(monitorDimensions, _newlyAddedItemsToLoadAndWrap);
        }

        public void ReloadWrappedItems(MonitorDimensions monitorDimensions)
        {
            var clipboardItemIds = base.ClipboardManager.GetAllItemIds();
            
            AreClipboardItemsAvailable = false;

            IEnumerable<string> allItemIds = base.ClipboardManager.GetAllItemIds();
            base.ClipboardViewItemWrappers.Clear();
            base.LoadAndWrapItems(monitorDimensions, allItemIds, pinnedOnly: true);

            AreClipboardItemsAvailable = true;
        }

        public void LoadViewItems(MonitorDimensions monitorDimensions)
        {
            var clipboardItemIds = base.ClipboardViewItemWrappers.Select(w => w.Id).ToList();

            foreach(string clipboardItemId in clipboardItemIds)
            {
#if DEBUG
                Debug.WriteLine(clipboardItemId);
#endif
                base.LoadViewItem(clipboardItemId, monitorDimensions);

            }
        }

        public IClipboardItem GetClipboardItem(string id, bool restoreShrinked)
        {
            return base.ClipboardManager.GetClipboardItem(id, restoreShrinked);
        }


    }
}
