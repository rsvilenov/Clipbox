using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views.Settings.BuiltInExtensions.UIParts;
using Clipbox.Views.Settings.Interfaces;
using System;

namespace Clipbox.Views.Settings.BuiltInExtensions
{
    public class CloudBuiltInSettingsUIExtension : BuiltInSettingsUIExtension, ICloudSyncServiceConsumer
    {
        private AutoCloudSyncPinnedItemsSettingUIPart _autoCloudSyncPinnedItemsSettingUIPart;
        private CloudSupportSelectorSettingUIPart _cloudSupportSelectorSettingUIPart;
        private IsCloudSyncEnabledSettingUIPart _isCloudSyncEnabledSettingUIPart;

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
                RequestSettingChange(SettingsKeys.IsCloudServicesEnabled, cloudSupportExtSelected);
            }
            else if (settingKey == SettingsKeys.IsCloudServicesEnabled)
            {

                if (!GetSetting<bool>(SettingsKeys.IsCloudServicesEnabled))
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
            if (settingKey == SettingsKeys.IsCloudServicesEnabled 
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
            _isCloudSyncEnabledSettingUIPart = new IsCloudSyncEnabledSettingUIPart();

            builder
                .Add(SettingsKeys.IsCloudServicesEnabled, new AreCloudServicesEnabledSettingUIPart())
                .Add(SettingsKeys.SelectedCloudSupportServiceName, _cloudSupportSelectorSettingUIPart)
                .Add(SettingsKeys.IsCloudSyncEnabled, _isCloudSyncEnabledSettingUIPart)
                .Add(SettingsKeys.IsAutomaticCloudSyncOfPinnedItemsEnabled, _autoCloudSyncPinnedItemsSettingUIPart);
        }

        private void ManageUIPartsVisibility(ISettingKey settingKey)
        {
            bool areCloudSvcsEnabled = GetSetting<bool>(SettingsKeys.IsCloudServicesEnabled);
            if (settingKey == SettingsKeys.IsCloudServicesEnabled)
            {
                _isCloudSyncEnabledSettingUIPart.SetIsVisible(areCloudSvcsEnabled);
                _cloudSupportSelectorSettingUIPart.SetIsVisible(areCloudSvcsEnabled);
                _autoCloudSyncPinnedItemsSettingUIPart?.SetIsVisible(areCloudSvcsEnabled);
            }
        }

        public void SetCloudSyncService(ICloudSynchronizationService cloudSyncSvc)
        {
            _cloudSupportSelectorSettingUIPart.SetCloudSyncService(cloudSyncSvc);
        }

        protected override void Dispose(bool disposing)
        {
            _cloudSupportSelectorSettingUIPart?.Dispose();
        }
    }
}
