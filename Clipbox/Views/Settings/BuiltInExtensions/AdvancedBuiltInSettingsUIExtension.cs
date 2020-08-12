using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class AdvancedBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        private ExternalSearchProcessSettingUIPart _extenralSearchProcessSettingUIPart;
        private MaxPreviewTasksSettingUIPart _maxPreviewTasksSettingUIPart;

        public override SettingsCategory Category => SettingsCategory.Advanced;

        protected override void OnSettingUpdated(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);

            if (settingKey == SettingsKeys.RunMainProcessWithHighPriority
                || settingKey == SettingsKeys.UseSearchProviderInExternalProcess)
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
            _maxPreviewTasksSettingUIPart = new MaxPreviewTasksSettingUIPart();

            builder
                .Add(SettingsKeys.EnabledExtensions, new EnabledExtensionsSettingUIPart())
                .Add(SettingsKeys.PreviewItemHeightRatio, new ScreenHightItemHeightRatioSettingUIPart())
                .Add(SettingsKeys.IsBackgroundPreviewGenerationEnabled, new BackgroundPreviewGenerationSettingUIPart())
                .Add(SettingsKeys.PreviewGenerationTimeout, new PreviewGenerationTimeoutSettingUIPart())
                .Add(SettingsKeys.PreviewCacheMaxSize, new PreviewCacheMaxSizeSettingUIPart())
                .Add(SettingsKeys.AutoDetermineMaxSimultaneousPreviewTasks, new AutoDetermineMaxPreviewTasksSettingUIPart())
                .Add(SettingsKeys.MaxSimultaneousPreviewTasks, _maxPreviewTasksSettingUIPart)
                .Add(SettingsKeys.NumOfNotYetVisibleRowsToPreload, new NumOfRowsToPreloadSettingUIPart())
                .Add(SettingsKeys.MaxNumOfItemToLoadAtOnce, new NumItemsOnPageSettingUIPart())
                .Add(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi, new SizeToDpiSettingUIPart())
                .Add(SettingsKeys.IsSearchEnabled, new EnableSearchSettingUIPart())
                .Add(SettingsKeys.UseSearchProviderInExternalProcess, _extenralSearchProcessSettingUIPart)
                .Add(SettingsKeys.RunMainProcessWithHighPriority, new MainProcessPrioritySettingUIPart())
                .Add(SettingsKeys.OnItemStoringFlowExtensionsTimeoutMs, new StoreItemTimeoutSettingUIPart())
                .Add(SettingsKeys.OnItemStoringFlowExtensionsMaxTasks, new StoreMaxTasksSettingUIPart())
                .Add(SettingsKeys.AreFadeAnimationsEnabled, new FadeAnimationsSettingUIPart());
        }

        private void ManageUIPartsVisibility(ISettingKey settingKey)
        {
            if (settingKey == SettingsKeys.IsSearchEnabled)
            {
                _extenralSearchProcessSettingUIPart?.SetIsVisible(GetSetting<bool>(SettingsKeys.IsSearchEnabled));
            }
            else if (settingKey == SettingsKeys.AutoDetermineMaxSimultaneousPreviewTasks)
            {
                _maxPreviewTasksSettingUIPart?.SetIsVisible(!GetSetting<bool>(SettingsKeys.AutoDetermineMaxSimultaneousPreviewTasks));
            }
        }
    }
}
