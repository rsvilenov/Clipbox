using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class AdvancedBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        private ExternalSearchProcessSettingUIPart _extenralSearchProcessSettingUIPart;

        public override SettingsCategory Category => SettingsCategory.Advanced;

        protected override void OnSettingUpdated(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);

            if (settingKey == SettingsKeys.RunMainProcessWithHighPriority)
            {
                RequestAppRestart();
            }
        }

        protected override void OnInitialSettingReceived(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);
        }
        
        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            _extenralSearchProcessSettingUIPart = new ExternalSearchProcessSettingUIPart();
           
            builder
                .Add(SettingsKeys.AreFadeAnimationsEnabled, new FadeAnimationsSettingUIPart())
                .Add(SettingsKeys.IsBackgroundPreviewGenerationEnabled, new BackgroundPreviewGenerationSettingUIPart())
                .Add(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi, new SizeToDpiSettingUIPart())
                .Add(SettingsKeys.IsSearchEnabled, new EnableSearchSettingUIPart())
                .Add(SettingsKeys.UseSearchProviderInExternalProcess, _extenralSearchProcessSettingUIPart)
                .Add(SettingsKeys.RunMainProcessWithHighPriority, new MainProcessPrioritySettingUIPart())
                .Add(SettingsKeys.PreviewGenerationTimeout, new PreviewGenerationTimeoutSettingUIPart());
        }

        private void ManageUIPartsVisibility(ISettingKey settingKey)
        {
            if (settingKey == SettingsKeys.IsSearchEnabled)
            {
                _extenralSearchProcessSettingUIPart?.SetIsVisible(GetSetting<bool>(SettingsKeys.IsSearchEnabled));
            }
        }
    }
}
