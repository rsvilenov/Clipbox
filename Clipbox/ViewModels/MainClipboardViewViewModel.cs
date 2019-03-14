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

        private AsyncObservableCollection<ClipboardViewItem> _clipboardViewItems;

        public AsyncObservableCollection<ClipboardViewItem> ClipboardViewItems
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

        public MainClipboardViewViewModel()
        {
            
        }

        public void LoadItems()
        {
            const int NUMBER_OF_ITEMS = 1000;

            ClipboardViewItems = new AsyncObservableCollection<ClipboardViewItem>();

            int currentItemForTheDay = 0;
            int currentNumber = NUMBER_OF_ITEMS;
            for (int i = 0; i < NUMBER_OF_ITEMS; i++)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
                {
                    int itemsPerDay = new Random().Next(1, 30);
                    // by the time the dispatcher executes this code, the counter 
                    // in the loop has already reached its maximum value, so we cannot
                    // count on "i" for getting the number of the element.
                    DateTime date = DateTime.Today.AddDays(currentNumber);
                    if (currentItemForTheDay >= itemsPerDay)
                    {

                        currentNumber--;
                        currentItemForTheDay = 0;
                    }

                    ClipboardViewItems.Add(new MainClipboardViewItem
                    {
                        DateCreated = date
                    });
                    currentItemForTheDay++;
                }));
                
            }
        }
    }
}
