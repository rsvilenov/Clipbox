using Clipbox.Infrastructure.UI;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemDimensions : BindableObject
    {
        private int _previewWidth;
        public int PreviewWidth
        {
            get { return _previewWidth; }
            set { SetField(ref _previewWidth, value); }
        }

        private int _previewHeight;
        public int PreviewHeight
        {
            get { return _previewHeight; }
            set { SetField(ref _previewHeight, value); }
        }
    }
}
