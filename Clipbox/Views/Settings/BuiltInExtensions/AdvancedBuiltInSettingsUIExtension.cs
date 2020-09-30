using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Infrastructure.UI;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;
using System;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class AdvancedBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        private ExternalSearchProcessSettingUIPart _extenralSearchProcessSettingUIPart;
        private MaxPreviewTasksSettingUIPart _maxPreviewTasksSettingUIPart;

        protected override SettingsCategory Category => SettingsCategory.Advanced;

        private readonly SlidingTimer _restartAppDialogSlidingTimer;

        public AdvancedBuiltInSettingsUIExtension()
        {
            _restartAppDialogSlidingTimer = new SlidingTimer
            {
                IntervalMs = 1000
            };

            _restartAppDialogSlidingTimer.Elapsed += _restartAppDialogSlidingTimer_Elapsed;
        }

        private void _restartAppDialogSlidingTimer_Elapsed(object sender, System.EventArgs e)
        {
            RequestAppRestart();
        }

        protected override void OnSettingUpdated(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);

            if (settingKey == SettingsKeys.RunMainProcessWithHighPriority
                || settingKey == SettingsKeys.UseSearchProviderInExternalProcess
                || settingKey == SettingsKeys.EnabledExtensions
                || settingKey == SettingsKeys.ResizeAccordingToMonitorOrSystemDpi
                || settingKey == SettingsKeys.AutoDetermineMaxSimultaneousPreviewTasks)
            {
                RequestAppRestart();
            }

            // for integer values (with the IntUpDown control) delay the restart request dialog
            if (settingKey == SettingsKeys.PreviewGenerationTimeout
                || settingKey == SettingsKeys.PreviewCacheMaxSize
                || settingKey == SettingsKeys.MaxSimultaneousPreviewTasks
                || settingKey == SettingsKeys.OnItemStoringFlowExtensionsTimeoutMs
                || settingKey == SettingsKeys.OnItemStoringFlowExtensionsMaxTasks
                || settingKey == SettingsKeys.ClipboardFormatSupportExtTimeout
                || settingKey == SettingsKeys.ClipboardListenningThrottleInterval
                || settingKey == SettingsKeys.ClipboardOpenningTimeoutMs)
            {
                _restartAppDialogSlidingTimer.Slide();
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
                .Add(SettingsKeys.AreFadeAnimationsEnabled, new FadeAnimationsSettingUIPart())
                .Add(SettingsKeys.CloudServiceAuthAttemptTimeout, new CloudAuthTimeoutSettingUIPart())
                .Add(SettingsKeys.ClipboardFormatSupportExtTimeout, new ClipboardFormatSupportExtTimeoutSettingUIPart())
                .Add(SettingsKeys.ClipboardOpenningTimeoutMs, new ClipboardOpeningTimeoutSettingUIPart());
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

        protected override void Dispose(bool disposing)
        {
            _restartAppDialogSlidingTimer.Dispose();
        }
    }
}
