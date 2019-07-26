using Clipbox.Infrastructure.UI;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewItemDimensions : BindableObject
    {
        private double _previewWidth;
        public double PreviewWidth
        {
            get { return _previewWidth; }
            set { SetField(ref _previewWidth, value); }
        }

        private double _previewHeight;
        public double PreviewHeight
        {
            get { return _previewHeight; }
            set { SetField(ref _previewHeight, value); }
        }
    }
}
