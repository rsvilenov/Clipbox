using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Enums;
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


        PreferredColorSettingUIPart _foregroundSelector;
        PreferredColorSettingUIPart _backgroundSelector;

        public TextPreviewGeneratorsSettingsUIExtension()
        {
            _previewGenerator = new RtfPreviewGenerator();
        }

        public override SettingsCategory Category => SettingsCategory.Previews;

        public override IPreviewData GetResultPreview(PreviewParams previewParams)
        {
            string sampleRtf = Resources.SampleData.RtfForSamplePreview;

            RichTextIntermediateFormat intFormat = new RichTextIntermediateFormat(sampleRtf);

            base.PassSettingsTo(_previewGenerator);
            return _previewGenerator.GetPreview(intFormat, previewParams);
        }
        
        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            if (settingName == ExtensionSettingNames.PreserveOriginalFontProperties
                && _backgroundSelector != null
                & _foregroundSelector != null)
            {
                AdjustColorSelectorsProperties();
            }

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
                .Add<bool, RtfPreviewGenerator>(ExtensionSettingNames.StretchBackgroundToFill, false)

                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.PreferredBackground, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.PreferredForeground, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.FontScaleFactor, 14d)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.Compactify, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.StretchBackgroundToFill, false)

                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreferredBackground, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreferredForeground, null)
                .Add<string, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FormattingCharsForeground, null)
                .Add<double, UnicodeTextPreviewGenerator>(ExtensionSettingNames.FontScaleFactor, 14d)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.Compactify, false)
                .Add<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.StretchBackgroundToFill, false);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            _foregroundSelector = new PreferredColorSettingUIPart(isForegroundSelector: true);
            _backgroundSelector = new PreferredColorSettingUIPart(isForegroundSelector: false);

            AdjustColorSelectorsProperties();

            builder
                .Add(ExtensionSettingNames.IncludeAnnotation, new IncludeTextAnnotationsSettingUIPart())
                .Add(ExtensionSettingNames.StretchBackgroundToFill, new StretchBackgroundSettingUIPart())
                .Add(ExtensionSettingNames.Compactify, new CompactifySettingUIPart())
                .Add(ExtensionSettingNames.PreserveOriginalFontProperties, new PreserveFontPropsSettingUIPart())
                .Add(ExtensionSettingNames.PreferredBackground, _backgroundSelector)
                .Add(ExtensionSettingNames.PreferredForeground, _foregroundSelector)
                .Add(ExtensionSettingNames.FontScaleFactor, new TextScaleSettingUIPart());
        }

        private void AdjustColorSelectorsProperties()
        {
            if (GetSetting<bool, RtfPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties))
            {
                _backgroundSelector.Visibility = System.Windows.Visibility.Collapsed;
                _foregroundSelector.Visibility = System.Windows.Visibility.Collapsed;

                _backgroundSelector.SetToDefaultColor();
                _foregroundSelector.SetToDefaultColor();
            }
            else
            {
                _backgroundSelector.Visibility = System.Windows.Visibility.Visible;
                _foregroundSelector.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }
}
