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
    [IntermediateFormatSupport("{D8F8C369-7F81-4138-9718-4F34EB1BD669}")]
    // set the priority of this preview generator to a high value,
    // so it takes priority over the generators of the other text formats,
    // since the rtfs contain the most information, meaningful to the user
    [PreviewPriority(100)]
    public class RtfPreviewGenerator : SinglePreviewGenerator<RichTextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(RichTextIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
        {
            ImagePreviewData preview = null;

            string rtf = intermediateFormat.Rtf;

            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        preview = TextPreviewCreator.GeneratePreview(rtf, TextPreviewCreator.TextFormat.Rtf, previewWidth, previewHeight);
                    }));
            }

            return preview;
        }
        
    }
}
