using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.UI;
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
    public class ClipboardViewViewModel : BindableObject
    {
        public ICommand ClearClipboardEntries { get; set; }

        public ICommand DeleteClipboardEntry { get; set; }

        public ICommand PinUnpinClipboardEntry { get; set; }

        public event EventHandler<RequestPreviewDimentionsEventArgs> OnPreviewDimentionsRequested;
        
        private IClipboardFormatPreviewExtentionContainer _previewContainer;

        private IClipboardManager _clipboardManager;

        ISettingsService _settingsService;

        private AsyncObservableCollection<ClipboardViewItem> _clipboardViewItems;

        public AsyncObservableCollection<ClipboardViewItem> ClipboardViewItems
        {
            get { return _clipboardViewItems; }
            set { SetField(ref _clipboardViewItems, value); }
        }
        
        private List<ClipboardViewItem> _selectedClipboardViewItems;

        public List<ClipboardViewItem> SelectedClipboardViewItems
        {
            get { return _selectedClipboardViewItems; }
            set { SetField(ref _selectedClipboardViewItems, value); }
        }

        private bool _areClipboardItemsAvailable;

        public bool AreClipboardItemsAvailable
        {
            get { return _areClipboardItemsAvailable; }
            set { SetField(ref _areClipboardItemsAvailable, value); }
        }
        
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

        public ClipboardViewViewModel(IClipboardManager clipboardManager, IClipboardFormatPreviewExtentionContainer previewsContainer, ISettingsService settingsService)
        {
            ClearClipboardEntries = new DelegateCommand(ClearAllClipboardEntriesHandler);
            DeleteClipboardEntry = new DelegateCommand<ClipboardViewItem>(DeleteClipboardEntryHandler);
            PinUnpinClipboardEntry = new DelegateCommand<ClipboardViewItem>(PinUnpinClipboardEntryHandler);

            ClipboardViewItems = new AsyncObservableCollection<ClipboardViewItem>();
            SelectedClipboardViewItems = new List<ClipboardViewItem>();

            _previewContainer = previewsContainer;
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;
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
                _clipboardManager.PinUnpinItemItem(obj.ClipboardItemId, obj.IsPinned);
            }));
            
        }

        private void DeleteClipboardEntryHandler(ClipboardViewItem parameter)
        {
            _clipboardManager.RemoveClipboardItem(parameter.ClipboardItemId);
        }

        private void ClearAllClipboardEntriesHandler()
        {
            _clipboardManager.Clear();
            _clipboardManager.ClearSystemClipboard();
            ClipboardViewItems.Clear();
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
            var itemToRemove = ClipboardViewItems.FirstOrDefault(item => item.ClipboardItemId == clipboardItemId);
            if (itemToRemove != null)
            {
                ClipboardViewItems.Remove(itemToRemove);
                return true;
            }

            return false;
        }

        private void AddNewItem(string clipboardItemId)
        {
            var args = new RequestPreviewDimentionsEventArgs();

            if (OnPreviewDimentionsRequested == null)
            {
                Debug.WriteLine("WARN: OnPreviewDimentionsRequested has no subscribers.");
                return;
            }

            OnPreviewDimentionsRequested?.Invoke(this, args);

            if (args.PreviewHeight == 0)
            {
                Debug.WriteLine("WARN: args.PreviewHeight is 0.");
                return;
            }

            int availableItemWidth = (int)args.PreviewWidth;
            int availableItemHeight = (int)args.PreviewHeight;

            IClipboardItem item = GetClipboardItem(clipboardItemId, restoreShrinked: true);
            if (ClipboardViewItems.Any(cvi => cvi.ClipboardItemId == clipboardItemId))
            {
                throw new InvalidOperationException("Item with the same key has already been added to the preview item collection.");
            }

            AreClipboardItemsAvailable = true;

            int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);
            ClipboardViewItemFactory viewItemFactory = new ClipboardViewItemFactory(_previewContainer, maxPreviewItemsInGroupItem);
            var viewItem = viewItemFactory.FromClipboardItem(item, availableItemWidth, availableItemHeight);

            ClipboardViewItems.Add(viewItem);
        }

        public void ReloadItems()
        {
            var args = new RequestPreviewDimentionsEventArgs();

            if (OnPreviewDimentionsRequested == null)
            {
                Debug.WriteLine("WARN: OnPreviewDimentionsRequested has no subscribers.");
                return;
            }

            OnPreviewDimentionsRequested?.Invoke(this, args);

            if (args.PreviewHeight == 0)
            {
                Debug.WriteLine("WARN: args.PreviewHeight is 0.");
                return;
            }

            int availableItemWidth = (int)args.PreviewWidth;
            int availableItemHeight = (int)args.PreviewHeight;

            var clipboardItemIds = _clipboardManager.GetAllItemIds();
            

            ClipboardViewItems.Clear();
            AreClipboardItemsAvailable = false;

            int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);
            ClipboardViewItemFactory viewItemFactory = new ClipboardViewItemFactory(_previewContainer, maxPreviewItemsInGroupItem);
            foreach (var clipboardItemId in clipboardItemIds)
            {
    #if DEBUG
                Debug.WriteLine(clipboardItemId);
#endif
                IClipboardItem item = GetClipboardItem(clipboardItemId, restoreShrinked: true);
                var viewItem = viewItemFactory.FromClipboardItem(item, availableItemWidth, availableItemHeight);
                ClipboardViewItems.Add(viewItem);

                AreClipboardItemsAvailable = true;
            }

#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif
        }

        public IClipboardItem GetClipboardItem(string id, bool restoreShrinked)
        {
            return _clipboardManager.GetClipboardItem(id, restoreShrinked);
        }


    }
}
