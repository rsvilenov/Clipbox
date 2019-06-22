using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.ViewModels.SerializableObjects;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardPreviewItem : BindableObject
    {
        // hide the default constructor - we will construct these objects with static factory methods
        private ClipboardPreviewItem() { }

        public static ClipboardPreviewItem FromStoredPreviewItem(StoredPreviewItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var cpi = new ClipboardPreviewItem
            {
                Text = item.Text,
                Description = item.Description
            };

            if (item.ImageData != null)
            {
                using (var ms = new MemoryStream(item.ImageData))
                using (Bitmap bmp = new Bitmap(ms))
                {
                    cpi.Image = bmp.ToBitmapSource();
                }
            }

            return cpi;
        }

        /// <summary>
        /// Generates ClipboardPreviewItem from FilePreviewData
        /// and disposes the image inside the FilePreviewData.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ClipboardPreviewItem FromFilePreviewData(FilePreviewData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var instance = new ClipboardPreviewItem();
            instance.Description = data.Path;
            instance.Text = data.Annotation;
            if (data.Preview != null)
            {
                if (data.Preview is Bitmap)
                {
                    instance.Image = (data.Preview as Bitmap).ToBitmapSource();
                    data.Preview.Dispose();
                }
                else
                {
                    throw new FormatException("The preview image, if present, should be Bitmap.");
                }
            }
            else
            {
                instance.Description += " (no preview)";
                instance.Image = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/warn_triangle.png"));
            }

            return instance;
        }

        /// <summary>
        /// Generates ClipboardPreviewItem from ImagePreviewData
        /// and disposes the image in the ImagePreviewData.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ClipboardPreviewItem FromImagePreviewData(ImagePreviewData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var instance = new ClipboardPreviewItem();
            instance.Text = data.Annotation;

            if (data.Preview != null)
            {
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
                    throw new FormatException("Image, if present, should be either Bitmap or Metafile");
                }

                data.Preview.Dispose();
            }
            else
            {
                instance.Description += " (no preview)";
                instance.Image = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/warn_triangle.png"));
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
