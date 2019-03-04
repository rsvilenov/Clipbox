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
            for (int i = 0; i < 300; i++)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
                {
                    // by the time the dispatcher executes this code, the counter 
                    // in the loop has already reached its maximum value, so we cannot
                    // count on "i" for getting the number of the element.
                    int currentNumber = ClipboardViewItems.Count;
                    ClipboardViewItems.Add(new MainClipboardViewItem
                    {
                        DateCreated = DateTime.Now.AddDays(currentNumber * -1)
                    });
                }));
                
            }
        }
    }
}
