using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extentions.BuiltIn.TextFormats.PreviewGenerators.Common;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Windows.Threading;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PreviewGenerators
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    public class TextPreviewGenerator : SinglePreviewGenerator<TextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(TextIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
        {
            ImagePreviewData preview = null;

            string text = intermediateFormat.Text;

            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        preview = TextFormatsPreviewCreator.GeneratePreview(text, TextFormatsPreviewCreator.TextFormat.Text, previewWidth, previewHeight);
                    }));
            }

            return preview;
        }

    }
}
