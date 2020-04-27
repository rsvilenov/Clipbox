using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators.Common;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Windows.Threading;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators
{
    [ClipboxExtensionAttribute(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextPreviewGenerator : SinglePreviewGenerator<UnicodeTextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(UnicodeTextIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            ImagePreviewData preview = null;

            string text = intermediateFormat.Text;

            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        preview = TextFormatsPreviewCreator.GeneratePreview(text, 
                            TextFormatsPreviewCreator.TextFormat.Text, 
                            desiredPreviewWidth: previewParams.PreviewWidth,
                            maxPreviewHeight: previewParams.PreviewHeight,
                            dpi: previewParams.DisplayDpi,
                            areDimensionsScaledToDpi: previewParams.AreDimensionsScaledToDpi);
                    }));
            }

            return preview;
        }
    }
}
