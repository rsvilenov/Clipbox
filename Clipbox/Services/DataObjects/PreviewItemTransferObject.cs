using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Services.DataObjects
{
    [Serializable]
    public class PreviewItemTransferObject
    {
        public byte[] BitmapData { get; set; }
        
        public string Text { get; set; }

        public string Description { get; set; }

        public PreviewDimensions PreviewDimensions { get; set; }

        public static PreviewItemTransferObject FromFilePreviewData(FilePreviewData filePreviewData)
        {
            byte[] bitmapData = null;
            Size bmpSize = Size.Empty;

            if (filePreviewData.Preview != null)
            {
                if (filePreviewData.Preview is Bitmap)
                {
                    Bitmap bmp = (Bitmap)filePreviewData.Preview;
                    bmpSize = bmp.Size;
                    bitmapData = bmp.ToByteArray();
                }
                else
                {
                    throw new FormatException("The preview image, if present, should be Bitmap.");
                }
            }

            return new PreviewItemTransferObject
            {
                Description = filePreviewData.Path,
                Text = filePreviewData.Annotation,
                BitmapData = bitmapData,
                PreviewDimensions = bmpSize == Size.Empty ? null : PreviewDimensions.FromDrawingSize(bmpSize)
            };
        }

        public static PreviewItemTransferObject FromImagePreviewData(ImagePreviewData imagePreviewData)
        {
            byte[] bitmapData = null;
            Size bmpSize = Size.Empty;

            if (imagePreviewData.Preview != null)
            {
                if (imagePreviewData.Preview is Bitmap)
                {
                    Bitmap bmp = (Bitmap)imagePreviewData.Preview;
                    bmpSize = bmp.Size;
                    bitmapData = bmp.ToByteArray();
                }
                else if (imagePreviewData.Preview is Metafile)
                {
                    Bitmap bmp = ((Metafile)imagePreviewData.Preview).ToBitmap();
                    if (bmp != null)
                    {
                        bmpSize = bmp.Size;
                        bitmapData = bmp.ToByteArray();
                    }
                }
                else
                {
                    throw new FormatException("Image, if present, should be either Bitmap or Metafile");
                }
            }

            return new PreviewItemTransferObject
            {
                Text = imagePreviewData.Annotation,
                BitmapData = bitmapData,
                PreviewDimensions = bmpSize == Size.Empty ? null : PreviewDimensions.FromDrawingSize(bmpSize)
            };
        }
    }
}
