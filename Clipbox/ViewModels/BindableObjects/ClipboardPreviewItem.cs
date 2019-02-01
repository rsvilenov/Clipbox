using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.UI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardPreviewItem : BindableObject
    {
        // hide the default constructor - we will construct these objects with static factory methods
        private ClipboardPreviewItem() { }

        public static ClipboardPreviewItem FromFilePreviewData(FilePreviewData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Preview == null)
            {
                throw new ArgumentNullException("data.PreviewData");
            }

            var instance = new ClipboardPreviewItem();
            instance.Description = data.Path;
            instance.Text = data.Annotation;
            if (data.Preview != null && data.Preview is Bitmap)
            {
                instance.Image = (data.Preview as Bitmap).ToBitmapSource();
            }
            else
            {
                // TODO: default file icon
            }

            return instance;
        }

        public static ClipboardPreviewItem FromImagePreviewData(ImagePreviewData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Preview == null)
            {
                throw new ArgumentNullException("data.PreviewData");
            }

            var instance = new ClipboardPreviewItem();
            instance.Text = data.Annotation;

            if (data.Preview is Bitmap)
            {
                instance.Image = (data.Preview as Bitmap).ToBitmapSource();
            }
            else if (data.Preview is Metafile)
            {
                instance.Image = (data.Preview as Metafile).ToBitmapSource();
            }
            else
            {
                throw new FormatException("Image should be either Bitmap or Metafile");
            }
            
            return instance;
        }

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
