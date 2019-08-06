using Clipbox.Infrastructure.UI;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemDimensionInfo : BindableObject
    {
        private AsyncObservableCollection<ClipboardViewSubItemDimension> _dimensions;
        public AsyncObservableCollection<ClipboardViewSubItemDimension> Dimensions
        {
            get { return _dimensions; }
            set { SetField(ref _dimensions, value); }
        }

        private bool _isViewItemGroup;
        public bool IsViewItemGroup
        {
            get { return _isViewItemGroup; }
            set { SetField(ref _isViewItemGroup, value); }
        }

        private int _numberOfItemsWithoutPreview;
        public int NumberOfItemsWithoutPreview
        {
            get { return _numberOfItemsWithoutPreview; }
            set { SetField(ref _numberOfItemsWithoutPreview, value); }
        }
    }
}
