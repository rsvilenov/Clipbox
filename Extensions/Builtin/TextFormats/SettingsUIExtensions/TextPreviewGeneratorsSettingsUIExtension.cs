using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtensionAttribute]
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
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.SizeToDpi, false)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.Background, 14d)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.Foreground, 14d)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.SizeToDpi, false)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Background, 14d)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Foreground, 14d)
                .Add<double, RtfPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.SizeToDpi, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.PreserveFontProperties, true)
                .Add<double, RtfPreviewGenerator>(ExtensionSettingNames.Background, 14d)
                .Add<double, RtfPreviewGenerator>(ExtensionSettingNames.Foreground, 14d);

        }
    }
}
