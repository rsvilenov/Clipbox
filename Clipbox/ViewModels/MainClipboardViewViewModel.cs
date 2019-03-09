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


        public string _currentVisibleItemsDateString;

        public string CurrentVisibleItemsDateString
        {
            get { return _currentVisibleItemsDateString; }
            set { SetField(ref _currentVisibleItemsDateString, value); }
        }

        public MainClipboardViewViewModel()
        {
            
        }

        public void LoadItems()
        {
            ClipboardViewItems = new AsyncObservableCollection<ClipboardViewItem>();

            int currentItemForTheDay = 0;
            int currentNumber = 300;
            for (int i = 0; i < 300; i++)
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
