using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators.Common;
using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{D8F8C369-7F81-4138-9718-4F34EB1BD669}")]
    // set the priority of this preview generator to a high value,
    // so it takes priority over the generators of the other text formats,
    // since the rtfs contain the most information, meaningful to the user
    [PreviewPriority(100)]
    public class RtfPreviewGenerator : SinglePreviewGenerator<RichTextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(RichTextIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            var options = new TextFormatsPreviewCreator.PreviewGenerationOptions
            {
                InclideAnnotation = GetSetting<bool>(ExtensionSettingNames.IncludeAnnotation),
                PreserveRtfFormatting = GetSetting<bool>(ExtensionSettingNames.PreserveOriginalFontProperties),
                ShowFormattingCharacters = GetSetting<bool>(ExtensionSettingNames.ShowFormattingCharacters)
            };

            double fontSize = GetSetting<double>(ExtensionSettingNames.FontSize);
            if (GetSetting<bool>(ExtensionSettingNames.ScaleFontSizeToDpi))
            {
                double scaleFactor = previewParams.DisplayDpi / 96;
                fontSize *= scaleFactor;
            }
            options.FontSize = fontSize;

            string foregroundColorStr = GetSetting<string>(ExtensionSettingNames.Foreground);
            if (!string.IsNullOrEmpty(foregroundColorStr))
            {
                options.Foreground = (Color)ColorConverter.ConvertFromString(foregroundColorStr);
            }


            ImagePreviewData preview = null;

            string rtf = intermediateFormat.Rtf;

            using (OwnThreadDispatcher _dispatcher = new OwnThreadDispatcher())
            {
                _dispatcher.Invoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        preview = TextFormatsPreviewCreator.GeneratePreview(rtf, 
                            TextFormatsPreviewCreator.TextFormat.Rtf, 
                            desiredPreviewWidth: previewParams.PreviewWidth, 
                            maxPreviewHeight: previewParams.PreviewHeight, 
                            dpi: previewParams.DisplayDpi, 
                            areDimensionsScaledToDpi: previewParams.AreDimensionsScaledToDpi,
                            options: options);
                    }));
            }

            return preview;
        }
        
    }
}
