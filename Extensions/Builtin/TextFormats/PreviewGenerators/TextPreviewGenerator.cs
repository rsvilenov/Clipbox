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
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    public class TextPreviewGenerator : SinglePreviewGenerator<TextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(TextIntermediateFormat intermediateFormat, PreviewParams previewParams)
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
