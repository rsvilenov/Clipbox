using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;
using System;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class TextPreviewGeneratorsSettingsUIExtension : SettingsUIExtension
    {
        private RtfPreviewGenerator _previewGenerator;

        public TextPreviewGeneratorsSettingsUIExtension()
        {
            _previewGenerator = new RtfPreviewGenerator();
        }

        public override IPreviewData GetResultPreview(PreviewParams previewParams)
        {
            string sampleRtf = Resources.SampleData.RtfForSamplePreview;

            RichTextIntermediateFormat intFormat = new RichTextIntermediateFormat(sampleRtf);

            base.PassSettingsTo(_previewGenerator);
            return _previewGenerator.GetPreview(intFormat, previewParams);
        }
        
        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            base.InvalidateResultPreview();
        }

        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
                .Add<string, RtfPreviewGenerator>(ExtensionSettingNames.PreferredBackground, null)
                .Add<string, RtfPreviewGenerator>(ExtensionSettingNames.PreferredForeground, null)
                .Add<string, RtfPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, RtfPreviewGenerator>(ExtensionSettingNames.FontScaleFactor, 14d)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.Compactify, false)

                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.PreferredBackground, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.PreferredForeground, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.FontScaleFactor, 14d)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.Compactify, false)

                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreferredBackground, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreferredForeground, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FontScaleFactor, 14d)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Compactify, false);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder
                .Add(ExtensionSettingNames.Compactify, new CompactifySettingUIPart())
                .Add(ExtensionSettingNames.PreserveOriginalFontProperties, new PreserveFontPropsSettingUIPart())
                .Add(ExtensionSettingNames.FontScaleFactor, new TextScaleSettingUIPart())
                .Add(ExtensionSettingNames.PreferredBackground, new PreferredColorSettingUIPart(isForegroundSelector : false))
                .Add(ExtensionSettingNames.PreferredForeground, new PreferredColorSettingUIPart(isForegroundSelector: true));
        }
    }
}
