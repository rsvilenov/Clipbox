using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class TextPreviewGeneratorsSettingsUIExtension : SettingsUIExtension
    {
        public override UIElement GetSettingsViewPart()
        {
            return new Border
            {
                Width = 200,
                Height = 50,
                Background = Brushes.Green
            };
        }

        protected override void RegisterSettingKeys(SettingKeyCollectionBuilder settingKeyColBuilder)
        {
            settingKeyColBuilder
                .Add<string, RtfPreviewGenerator>(ExtensionSettingNames.Background, null)
                .Add<string, RtfPreviewGenerator>(ExtensionSettingNames.Foreground, null)
                .Add<double, RtfPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, false)

                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.Background, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.Foreground, null)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, false)

                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Background, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Foreground, null)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, false);

        }
    }
}
