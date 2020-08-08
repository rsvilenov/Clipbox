using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using System.Drawing;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtension(
    description: "Preview generation support for bitmap images (generates the thumbnail of the copied image)",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [IntermediateFormatSupport("{4422E0FD-3DD7-4B10-B167-DB63BB97D6E1}")]
    public class ImagePreviewGenerator : SinglePreviewGenerator<BitmapIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(BitmapIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            var previewData = new ImagePreviewData();

            using (Bitmap bmp = intermediateFormat.GetCopyOfBitmap())
            {
                if (bmp.Height > previewParams.PreviewHeight || bmp.Width > previewParams.PreviewWidth)
                {
                    previewData.Preview = bmp.GetScaled(previewParams.PreviewHeight * 2, previewParams.PreviewHeight); //img.GetThumbnailImage(previewWidth, previewHeight, () => false, IntPtr.Zero);
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
