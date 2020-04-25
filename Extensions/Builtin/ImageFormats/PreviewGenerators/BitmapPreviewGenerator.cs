using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using System.Drawing;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{4422E0FD-3DD7-4B10-B167-DB63BB97D6E1}")]
    public class ImagePreviewGenerator : SinglePreviewGenerator<BitmapIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(BitmapIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
        {
            var previewData = new ImagePreviewData();

            using (Bitmap bmp = intermediateFormat.GetCopyOfBitmap())
            {
                if (bmp.Height > previewHeight || bmp.Width > previewWidth)
                {
                    previewData.Preview = bmp.GetScaled(previewHeight * 2, previewHeight); //img.GetThumbnailImage(previewWidth, previewHeight, () => false, IntPtr.Zero);
                }
                else
                {
                    // Constructing a new object will copy all the
                    // pixel data anew, avoiding the typical locking problems with bitmaps...
                    previewData.Preview = new Bitmap(bmp);
                }

                previewData.Annotation = $"{bmp.Width} x {bmp.Height}";
            }

            return previewData;
        }
    }
}
