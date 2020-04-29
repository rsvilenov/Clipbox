using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators;
using System;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class TextPreviewGeneratorsSettingsUIExtension : SettingsUIExtension
    {
        private TextPreviewGeneratorsSettings _extensionUI;

        private TextPreviewGenerator _previewGenerator;

        public override UIElement GetSettingsViewPart(double maxWidth)
        {
            _previewGenerator = new TextPreviewGenerator();

            _extensionUI = new TextPreviewGeneratorsSettings
            {
                IsShowFormattingCharsToggled = GetSetting<bool, TextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters)
            };

            _extensionUI.SettingChanged += _extensionUI_SettingChanged;

            base.PassSettingsTo(_previewGenerator);

            return _extensionUI;
        }

        public override IPreviewData GetResultPreview(PreviewParams previewParams)
        {
            string sampleText = Resources.SampleData.TextForSamplePreview;

            TextIntermediateFormat intFormat = new TextIntermediateFormat(sampleText);

            return _previewGenerator.GetPreview(intFormat, previewParams);
        }

        private void _extensionUI_SettingChanged(object sender, System.EventArgs e)
        {
            RequestSettingChange<bool, TextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, _extensionUI.IsShowFormattingCharsToggled);
            RequestSettingChange<bool, RtfPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, _extensionUI.IsShowFormattingCharsToggled);
            RequestSettingChange<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, _extensionUI.IsShowFormattingCharsToggled);
        }
        
        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            base.PassSettingsTo(_previewGenerator);
        }

        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
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
