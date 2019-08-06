using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels.BindableObjects;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Clipbox.ViewModels.ClipboardViewItemTile
{
    public class MultiItemLoadingViewModel : BindableObject
    {
        public MultiItemLoadingViewModel()
        {
            _previewItems = new AsyncObservableCollection<ClipboardViewSubItemBase>();
            _previewItems.CollectionChanged += _previewItems_CollectionChanged;
        }

        private void _previewItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTotalClipboardItemsCount();
        }

        private AsyncObservableCollection<ClipboardViewSubItemBase> _previewItems;

        public AsyncObservableCollection<ClipboardViewSubItemBase> PreviewItems
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

        public int _numberOfItemsWithoutPreview;

        public int NumberOfItemsWithoutPreview
        {
            get { return _numberOfItemsWithoutPreview; }
            set
            {
                //UpdatePreviewItemsWithNMoreItem();
                SetField(ref _numberOfItemsWithoutPreview, value);
                UpdateTotalClipboardItemsCount();
            }
        }

        //private void UpdatePreviewItemsWithNMoreItem()
        //{
        //    var existingItem = _previewItems
        //            .FirstOrDefault(item => item is ClipboardViewSubItemNMore);

        //    if (existingItem != null)
        //    {
        //        if (NumberOfItemsWithoutPreview == 0)
        //        {
        //            _previewItems.Remove(existingItem);
        //            return;
        //        }

        //        ((ClipboardViewSubItemNMore)existingItem).NumberOfItems = NumberOfItemsWithoutPreview;

        //    }
        //    else
        //    {
        //        _previewItems.Add(new ClipboardViewSubItemNMore { NumberOfItems = NumberOfItemsWithoutPreview });
        //    }
        //}

        private void UpdateTotalClipboardItemsCount()
        {
            TotalClipboardItemsCount = PreviewItems
                .Count(item => item is ClipboardViewSubItemDimension)
                + NumberOfItemsWithoutPreview;
        }
        
    }
}
