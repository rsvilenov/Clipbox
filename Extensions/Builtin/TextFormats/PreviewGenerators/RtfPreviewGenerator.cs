using Clipbox.Core;
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
    [ClipboxExtension(
    description: "Preview generation support for RTF textt (generates a thumbnail of the text, rendered with its formatting)",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [IntermediateFormatSupport("{D8F8C369-7F81-4138-9718-4F34EB1BD669}")]
    // set the priority of this preview generator to a high value,
    // so it takes priority over the generators of the other text formats,
    // since the rtfs contain the most information, meaningful to the user
    [PreviewPriority(100)]
    public class RtfPreviewGenerator : SinglePreviewGenerator<RichTextIntermediateFormat, ImagePreviewData>
    {
        private const int MAX_RTF_LENGTH = 100000;

        protected override ImagePreviewData GetPreview(RichTextIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            if (intermediateFormat.Rtf.Length > MAX_RTF_LENGTH)
            {
                return null;
            }

            var options = new TextFormatsPreviewCreator.PreviewGenerationOptions
            {
                InclideAnnotation = GetSetting<bool>(ExtensionSettingNames.IncludeAnnotation),
                PreserveRtfFormatting = GetSetting<bool>(ExtensionSettingNames.PreserveOriginalFontProperties),
                MakeTextCompact = GetSetting<bool>(ExtensionSettingNames.Compactify),
                StretchBackgroundToFill = GetSetting<bool>(ExtensionSettingNames.StretchBackgroundToFill)
            };

            double fontScaleFactor = GetSetting<double>(ExtensionSettingNames.FontScaleFactor);
            if (GetSetting<bool>(ExtensionSettingNames.ScaleFontSizeToDpi))
            {
                double scaleFactor = previewParams.DisplayDpi / 96;
                fontScaleFactor *= scaleFactor;
            }
            options.FontScaleFactor = fontScaleFactor;

            string foregroundColorStr = GetSetting<string>(ExtensionSettingNames.PreferredForeground);
            if (!string.IsNullOrEmpty(foregroundColorStr))
            {
                options.Foreground = (Color)ColorConverter.ConvertFromString(foregroundColorStr);
            }

            string backgroundColorStr = GetSetting<string>(ExtensionSettingNames.PreferredBackground);
            if (!string.IsNullOrEmpty(backgroundColorStr))
            {
                options.Background = (Color)ColorConverter.ConvertFromString(backgroundColorStr);
            }

            string formattingCharsForegroundColorStr = GetSetting<string>(ExtensionSettingNames.FormattingCharsForeground);
            if (!string.IsNullOrEmpty(formattingCharsForegroundColorStr))
            {
                options.FormattingCharsForeground = (Color)ColorConverter.ConvertFromString(formattingCharsForegroundColorStr);
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
                            isTextBeyondCutoff: false,
                            options: options);
                    }));
            }

            return preview;
        }
        
    }
}
