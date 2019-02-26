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
                    ClipboardViewItems.Add(new MainClipboardViewItem());
                }));
                
            }
        }
    }
}
