using Clipbox.Infrastructure.UI;
using System.Windows.Media.Imaging;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardPreviewItem : BindableObject
    {
        private BitmapSource _image;

        public BitmapSource Image
        {
            get { return _image; }
            set { SetField(ref _image, value); }
        }

        private string _text;

        public string Text
        {
            get { return _text; }
            set { SetField(ref _text, value); }
        }

        // TODO: rename
        private string _description;
        
        public string Description
        {
            get { return _description; }
            set { SetField(ref _description, value); }
        }
    }
}
