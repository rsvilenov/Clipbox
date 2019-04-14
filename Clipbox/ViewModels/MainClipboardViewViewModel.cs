using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clipbox.ViewModels
{
    public class MainClipboardViewViewModel : BindableObject
    {
        private class MainClipboardViewItem : ClipboardViewItem
        {

        }

        public class ClipboardViewItemWrapper : BindableObject
        {
            public ClipboardViewItemWrapper(IClipboardItem clipboardItem)
            {
                // store the original item for searching...
                // TODO: map this item's properties to a viewmodel object
                ClipboardItem = clipboardItem;

                Id = clipboardItem.Id;
                DateCreated = new DateTime(clipboardItem.EntryDateTicks);
            }

            /// <summary>
            /// This gets called when the item scrolls into view
            /// </summary>
            public void GenerateFullViewItem()
            {

            }

            /// <summary>
            /// This gets called when the item scrolls out of view
            /// </summary>
            public void ShrinkFullViewItem()
            {
                
            }

            private string _id;

            public string Id
            {
                get { return _id; }
                private set { SetField(ref _id, value); }
            }

            private DateTime _dateCreated;
            
            public DateTime DateCreated
            {
                get { return _dateCreated; }
                private set
                {
                    SetField(ref _dateCreated, value);
                    DateCreatedFriendly = GetFriendlyDateString(value);
                }
            }

            private string _dateCreatedFriendly;

            public string DateCreatedFriendly
            {
                get { return _dateCreatedFriendly; }
                private set { SetField(ref _dateCreatedFriendly, value); }
            }

            private ClipboardViewItem _viewItem;

            public ClipboardViewItem ViewItem
            {
                get { return _viewItem; }
                private set { SetField(ref _viewItem, value); }
            }

            private bool _isViewItemLoaded;

            public bool IsViewItemLoaded
            {
                get { return _isViewItemLoaded; }
                private set { SetField(ref _isViewItemLoaded, value); }
            }

            /// <summary>
            /// We will not bind this item.
            /// </summary>
            public IClipboardItem ClipboardItem { get; private set; }

            
            private string GetFriendlyDateString(DateTime date)
            {
                // TODO: Translate 'Today' and 'Yesterday'.
                return
                    date.Date == DateTime.Today ?
                    "Today" :
                    date.Date == DateTime.Today.AddDays(-1) ?
                    "Yesterday" :
                    date.Year == DateTime.Now.Year ?
                        date.ToString("dd MMMM") :
                        date.ToString("dd MMMM yyyy");
            }

        }

        private AsyncObservableCollection<ClipboardViewItemWrapper> _clipboardViewItems;

        public AsyncObservableCollection<ClipboardViewItemWrapper> ClipboardViewItems
        {
            get { return _clipboardViewItems; }
            set { SetField(ref _clipboardViewItems, value); }
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

        private IClipboardManager _clipboardManager;

        public MainClipboardViewViewModel(IClipboardManager clipboardManager)
        {
            _clipboardManager = clipboardManager;
        }

        public void LoadItems()
        {
            //const int NUMBER_OF_ITEMS = 1000;

            ClipboardViewItems = new AsyncObservableCollection<ClipboardViewItemWrapper>();
            IEnumerable<string> allItemIds = _clipboardManager.GetAllItemIds();
            foreach (string clipboardItemId in allItemIds)
            {
                IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId);
                var tempViewItem = new ClipboardViewItemWrapper(clipboardItem);
                ClipboardViewItems.Add(tempViewItem);
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
    }
}
