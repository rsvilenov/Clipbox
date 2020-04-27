using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.PreviewGenerators
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{69BD2743-DADE-4B24-B3A1-5B6D73BDAE32}")]
    public class MetafilePreviewGenerator : SinglePreviewGenerator<MetafileIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(MetafileIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            var previewData = new ImagePreviewData();

            using (Metafile mf = intermediateFormat.GetCopyOfMetafile())
            {
                if (mf.Height > previewParams.PreviewHeight || mf.Width > previewParams.PreviewWidth)
                {
                    previewData.Preview = ((Image)mf).GetScaled(previewParams.PreviewHeight * 2, previewParams.PreviewHeight); //img.GetThumbnailImage(previewWidth, previewHeight, () => false, IntPtr.Zero);
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
