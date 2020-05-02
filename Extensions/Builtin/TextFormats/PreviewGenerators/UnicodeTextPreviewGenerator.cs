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
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextPreviewGenerator : SinglePreviewGenerator<UnicodeTextIntermediateFormat, ImagePreviewData>
    {
        protected override ImagePreviewData GetPreview(UnicodeTextIntermediateFormat intermediateFormat, PreviewParams previewParams)
        {
            var options = new TextFormatsPreviewCreator.PreviewGenerationOptions
            {
                InclideAnnotation = GetSetting<bool>(ExtensionSettingNames.IncludeAnnotation),
                PreserveRtfFormatting = GetSetting<bool>(ExtensionSettingNames.PreserveOriginalFontProperties),
                MakeTextCompact = GetSetting<bool>(ExtensionSettingNames.Compactify)
            };

            double fontScale = GetSetting<double>(ExtensionSettingNames.FontScale);
            if (GetSetting<bool>(ExtensionSettingNames.ScaleFontSizeToDpi))
            {
                double scaleFactor = previewParams.DisplayDpi / 96;
                fontScale *= scaleFactor;
            }
            options.FontScale = fontScale;

            string backgroundColorStr = GetSetting<string>(ExtensionSettingNames.Background);
            if (!string.IsNullOrEmpty(backgroundColorStr))
            {
                options.Foreground = (Color)ColorConverter.ConvertFromString(backgroundColorStr);
            }

            string foregroundColorStr = GetSetting<string>(ExtensionSettingNames.Foreground);
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
                            areDimensionsScaledToDpi: previewParams.AreDimensionsScaledToDpi,
                            options: options);
                    }));
            }

            return preview;
        }
    }
}
