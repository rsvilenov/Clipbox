using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class CloudBuiltInSettingsUIExtension : BuiltInSettingsUIExtension
    {
        private AutoCloudSyncPinnedItemsSettingUIPart _autoCloudSyncPinnedItemsSettingUIPart;
        private CloudSupportSelectorSettingUIPart _cloudSupportSelectorSettingUIPart;
        protected override SettingsCategory Category => SettingsCategory.Cloud;

        protected override void OnSettingUpdated(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);

            if (settingKey == SettingsKeys.SelectedCloudSupportServiceName)
            {
                bool cloudSupportExtSelected = !string.IsNullOrEmpty(GetSetting<string>(SettingsKeys.SelectedCloudSupportServiceName));

                // if the user has selected a cloud support extension,
                // request enabling of the cloud syncronization. If the
                // extension has been removed or no extension has been
                // selected, disable the syncronization.
                RequestSettingChange(SettingsKeys.IsCloudSyncEnabled, cloudSupportExtSelected);
            }
            else if (settingKey == SettingsKeys.IsCloudSyncEnabled)
            {

                if (!GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled))
                {
                    RequestSettingChange(SettingsKeys.SelectedCloudSupportServiceName, string.Empty);
                }
            }
        }

        protected override void OnInitialSettingReceived(ISettingKey settingKey)
        {
            ManageUIPartsVisibility(settingKey);
        }

        protected override void OnSettingUpdating(ISettingKey settingKey, object newValue, out bool cancel)
        {
            // if the user enables cloud syncing and no cloud support extension is selected,
            // cancel the change of the setting and show the cloud extension selector.
            if (settingKey == SettingsKeys.IsCloudSyncEnabled 
                && (bool)newValue
                && string.IsNullOrEmpty(GetSetting<string>(SettingsKeys.SelectedCloudSupportServiceName)))
            {
                _cloudSupportSelectorSettingUIPart?.SetIsVisible(true);
                cancel = true;
            }
            else
            {
                cancel = false;
            }
        }

        protected override void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder)
        {
            _autoCloudSyncPinnedItemsSettingUIPart = new AutoCloudSyncPinnedItemsSettingUIPart();
            _cloudSupportSelectorSettingUIPart = new CloudSupportSelectorSettingUIPart();

            builder
                .Add(SettingsKeys.IsCloudSyncEnabled, new IsCloudEnabledSettingUIPart())
                .Add(SettingsKeys.SelectedCloudSupportServiceName, _cloudSupportSelectorSettingUIPart)
                .Add(SettingsKeys.IsAutomaticCloudSyncOfPinnedItemsEnabled, _autoCloudSyncPinnedItemsSettingUIPart);
        }

        private void ManageUIPartsVisibility(ISettingKey settingKey)
        {
            bool isCloudEnabled = GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);
            if (settingKey == SettingsKeys.IsCloudSyncEnabled)
            {
                _cloudSupportSelectorSettingUIPart.SetIsVisible(isCloudEnabled);
                _autoCloudSyncPinnedItemsSettingUIPart?.SetIsVisible(isCloudEnabled);
            }
        }

    }
}
