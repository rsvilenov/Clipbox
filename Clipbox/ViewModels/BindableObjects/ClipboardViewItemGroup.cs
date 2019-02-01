using System.Collections.ObjectModel;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemGroup : ClipboardViewItem
    {
        public ClipboardViewItemGroup()
        {
            _previewItems = new ObservableCollection<ClipboardViewSubItemBase>();
        }

        private ObservableCollection<ClipboardViewSubItemBase> _previewItems;

        public ObservableCollection<ClipboardViewSubItemBase> PreviewItems
        {
            get { return _previewItems; }
            set { SetField(ref _previewItems, value); }
        }

        public int _totalClipboardItemsCount;

        public int TotalClipboardItemsCount
        {
            get { return _totalClipboardItemsCount; }
            set { SetField(ref _totalClipboardItemsCount, value); }
        }
    }
}
