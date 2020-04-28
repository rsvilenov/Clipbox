using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Extensions.BuiltIn.TextFormats.PreviewGenerators;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Infrastructure.UI.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

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
            
            SetSettingsToSamplePreviewGenerator();
            
            return _extensionUI;
        }

        public override UIElement GetResultPreview(PreviewParams previewParams)
        {
            string sampleText = Resources.SampleData.TextForSamplePreview;

            TextIntermediateFormat intFormat = new TextIntermediateFormat(sampleText);

            ImagePreviewData previewData = (ImagePreviewData)_previewGenerator.GetPreview(intFormat, previewParams);

            System.Windows.Controls.Image img = new System.Windows.Controls.Image();

            using (var bmp = (Bitmap)previewData.Preview)
            {
                img.BeginInit();
                img.Source = bmp.ToBitmapSource();
                img.EndInit();
            }

            return img;
        }

        private void _extensionUI_SettingChanged(object sender, System.EventArgs e)
        {
            RequestSettingChange<bool, TextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, _extensionUI.IsShowFormattingCharsToggled);
            RequestSettingChange<bool, RtfPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, _extensionUI.IsShowFormattingCharsToggled);
            RequestSettingChange<bool, UnicodeTextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, _extensionUI.IsShowFormattingCharsToggled);
        }

        private void SetSettingsToSamplePreviewGenerator(string settingName = null)
        {
            if (!(_previewGenerator is IConfigurationRecipient confRecipient))
            {
                throw new InvalidCastException($"The preview generator cannot receive settings. Please make it implement {nameof(IConfigurationRecipient)}");
            }

            SettingKeyCollectionBuilder settingKeyColBuilder = new SettingKeyCollectionBuilder();
            settingKeyColBuilder.Add<string, TextPreviewGenerator>(ExtensionSettingNames.Background, null)
                .Add<string, TextPreviewGenerator>(ExtensionSettingNames.Foreground, null)
                .Add<double, TextPreviewGenerator>(ExtensionSettingNames.FontSize, 14d)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.PreserveOriginalFontProperties, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ScaleFontSizeToDpi, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.IncludeAnnotation, false)
                .Add<bool, TextPreviewGenerator>(ExtensionSettingNames.ShowFormattingCharacters, false);

            foreach (ISettingKey key in settingKeyColBuilder.GetSettingKeys())
            {
                if (!string.IsNullOrEmpty(settingName) && key.Name != settingName)
                {
                    continue;
                }

                var method = typeof(SettingsUIExtension).GetMethod(nameof(GetSetting), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var generic = method.MakeGenericMethod(key.TypeOfValue, key.TargetComponentType);
                object value = generic.Invoke(this, new object[] { key.Name });

                confRecipient.PutSetting(key, value);
            }
        }
        
        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            SetSettingsToSamplePreviewGenerator(settingName);
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
