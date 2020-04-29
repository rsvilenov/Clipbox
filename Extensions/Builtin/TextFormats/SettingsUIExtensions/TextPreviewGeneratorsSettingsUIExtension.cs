using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.EventArgsClasses;
using System;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class TextPreviewGeneratorsSettingsUIExtension : SettingsUIExtension
    {
        private TextPreviewGeneratorsSettings _extensionUI;

        private RtfPreviewGenerator _previewGenerator;

        public override UIElement GetSettingsViewPart(double maxWidth)
        {
            _previewGenerator = new RtfPreviewGenerator();

            _extensionUI = new TextPreviewGeneratorsSettings
            {
                IsCompactifyToggled = GetSetting<bool, TextPreviewGenerator>(ExtensionSettingNames.Compactify),
                IsPreserveOriginalFontPropertiesToggled = GetSetting<bool, TextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties)
            };

            _extensionUI.SettingChanged += _extensionUI_SettingChanged;

            base.PassSettingsTo(_previewGenerator);

            return _extensionUI;
        }

        public override IPreviewData GetResultPreview(PreviewParams previewParams)
        {
            string sampleRtf = Resources.SampleData.RtfForSamplePreview;

            RichTextIntermediateFormat intFormat = new RichTextIntermediateFormat(sampleRtf);

            return _previewGenerator.GetPreview(intFormat, previewParams);
        }

        private void _extensionUI_SettingChanged(object sender, ExtensionSettingChangedEventArgs e)
        {
            foreach (Type t in new Type[] { typeof(TextPreviewGenerator), typeof(RtfPreviewGenerator), typeof(UnicodeTextPreviewGenerator) })
            {
                GetType()
                .GetMethod(nameof(RequestSettingChange), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .MakeGenericMethod(e.Value.GetType(), t)
                .Invoke(this, new object[] { e.SettingName, e.Value });
            }

            base.InvalidateResultPreview();
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
                .Add<string, RtfPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, RtfPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.Compactify, false)

                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.Background, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.Foreground, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.Compactify, false)

                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Background, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Foreground, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Compactify, false);
        }
    }
}
