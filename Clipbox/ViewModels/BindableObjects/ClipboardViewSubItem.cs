namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewSubItem : ClipboardViewSubItemBase
    {
        private ClipboardPreviewItem _previewItem;

        public ClipboardPreviewItem PreviewItem
        {
            get { return _previewItem; }
            set { SetField(ref _previewItem, value); }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetField(ref _isSelected, value); }
        }
    }
}
