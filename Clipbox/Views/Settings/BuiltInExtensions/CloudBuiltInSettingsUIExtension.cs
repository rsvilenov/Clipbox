using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class CloudBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        private AutoCloudSyncPinnedItemsSettingUIPart _autoCloudSyncPinnedItemsSettingUIPart;
        protected override SettingsCategory Category => SettingsCategory.Cloud;

        protected override void OnSettingUpdated(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);
        }

        protected override void OnInitialSettingReceived(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);
        }

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            _autoCloudSyncPinnedItemsSettingUIPart = new AutoCloudSyncPinnedItemsSettingUIPart();

            builder
                .Add(SettingsKeys.IsCloudSyncEnabled, new IsCloudEnabledSettingUIPart())
                .Add(SettingsKeys.IsAutomaticCloudSyncOfPinnedItemsEnabled, _autoCloudSyncPinnedItemsSettingUIPart);
        }

        private void ManageUIPartsVisibility(ISettingKey settingKey)
        {
            if (settingKey == SettingsKeys.IsCloudSyncEnabled)
            {
                _autoCloudSyncPinnedItemsSettingUIPart?.SetIsVisible(GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled));
            }
        }

    }
}
