using Clipbox.Infrastructure.UI;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemDimensionInfo : BindableObject
    {
        private AsyncObservableCollection<ClipboardViewItemDimensions> _dimensions;
        public AsyncObservableCollection<ClipboardViewItemDimensions> Dimensions
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
    }
}
