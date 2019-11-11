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
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextPreviewGenerator : SinglePreviewGenerator<UnicodeTextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(UnicodeTextIntermediateFormat intermediateFormat, int previewWidth, int previewHeight)
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
