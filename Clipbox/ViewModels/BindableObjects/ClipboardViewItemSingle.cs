namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemSingle : ClipboardViewItem
    {
        private ClipboardPreviewItem _previewItem;

        public ClipboardPreviewItem PreviewItem
        {
            get { return _previewItem; }
            set { SetField(ref _previewItem, value); }
        }
    }
}
