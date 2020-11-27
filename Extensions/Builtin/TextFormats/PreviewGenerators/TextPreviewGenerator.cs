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
        description: "Preview generation support for ANSI textt (generates a thumbnail of the text)",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    [PreviewPriority(98)]
    public class TextPreviewGenerator : SinglePreviewGenerator<TextIntermediateFormat, ImagePreviewData>
    {
        private const int TEXT_LENGTH_CUTOFF = 500;

        protected override ImagePreviewData GetPreview(TextIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            string text = null;
            bool isTextBeyondCutoff = false;
            if (intermediateFormat.Text.Length > TEXT_LENGTH_CUTOFF)
            {
                text = intermediateFormat.Text.Substring(0, TEXT_LENGTH_CUTOFF);
                isTextBeyondCutoff = true;
            }
            else
            {
                text = intermediateFormat.Text;
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

            string backgroundColorStr = GetSetting<string>(ExtensionSettingNames.PreferredBackground);
            if (!string.IsNullOrEmpty(backgroundColorStr))
            {
                options.Background = (Color)ColorConverter.ConvertFromString(backgroundColorStr);
            }

            string foregroundColorStr = GetSetting<string>(ExtensionSettingNames.PreferredForeground);
            if (!string.IsNullOrEmpty(foregroundColorStr))
            {
                options.Foreground = (Color)ColorConverter.ConvertFromString(foregroundColorStr);
            }

            string formattingCharsForegroundColorStr = GetSetting<string>(ExtensionSettingNames.FormattingCharsForeground);
            if (!string.IsNullOrEmpty(formattingCharsForegroundColorStr))
            {
                options.FormattingCharsForeground = (Color)ColorConverter.ConvertFromString(formattingCharsForegroundColorStr);
            }

            ImagePreviewData preview = null;
            
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
                            areDimensionsScaledToDpi: previewParams.AreDimensionsScaledToDpi,
                            isTextBeyondCutoff: isTextBeyondCutoff,
                            options: options);
                    }));
            }

            return preview;
        }

    }
}
