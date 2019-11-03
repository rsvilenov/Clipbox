using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtentionMethods;
using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.PreviewGenerators
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{69BD2743-DADE-4B24-B3A1-5B6D73BDAE32}")]
    public class MetafilePreviewGenerator : SinglePreviewGenerator<MetafileIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(MetafileIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
        {
            var previewData = new ImagePreviewData();

            using (Metafile mf = intermediateFormat.GetCopyOfMetafile())
            {
                if (mf.Height > previewHeight || mf.Width > previewWidth)
                {
                    previewData.Preview = ((Image)mf).GetScaled(previewHeight * 2, previewHeight); //img.GetThumbnailImage(previewWidth, previewHeight, () => false, IntPtr.Zero);
                }
                else
                {
                    // The image is Metafile, which is tricky to work with,
                    // so it would be best if we convert it to Bitmap.
                    previewData.Preview = new Bitmap(mf);
                }

                previewData.Annotation = $"{mf.Width} x {mf.Height}";
            }

            return previewData;
        }
    }
}
