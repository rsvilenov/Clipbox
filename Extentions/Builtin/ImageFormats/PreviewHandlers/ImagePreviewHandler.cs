using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Infrastructure.ExtentionMethods;
using System;
using System.Drawing;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_BITMAP")]
    [ClipboardFormat("CF_ENHMETAFILE")]
    public class ImagePreviewHandler : PreviewHandler<ImagePreviewData>
    {
        public override ImagePreviewData GetPreview(object data, int previewWidth, int previewHeight)
        {
            Image img = data as Image;
            if (img == null)
            {
                throw new ArgumentException("data is not Image");
            }

            var previewData = new ImagePreviewData();
            if (img.Height > previewHeight || img.Width > previewWidth)
            {
                previewData.Preview = img.GetScaled(previewHeight * 2, previewHeight); //img.GetThumbnailImage(previewWidth, previewHeight, () => false, IntPtr.Zero);
            }
            else
            {
                previewData.Preview = (Image)img.Clone();
            }

            previewData.Annotation = $"{img.Width} x {img.Height}";

            return previewData;
        }
    }
}
