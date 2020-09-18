using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Extensions.BuiltIn.Cloud.Microsoft.CloudSupportExtensions;
using Clipbox.Extensions.BuiltIn.Cloud.Microsoft.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.Cloud.Microsoft.SettingsUIExtensions
{
    [ClipboxExtension(
           description: "Settings UI for Microsoft OneDrive cloud support",
           author: ProductConstants.PRODUCT_NAME,
           version: 1.0,
           MinHostVersion = 1.0
           )]
    public class GoogleDriveSettingsUIExtension : SettingsUIExtension
    {
        protected override SettingsCategory Category => SettingsCategory.Cloud;

        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
                .Add<bool, OneDriveCloudSupportExtension>(ExtensionSettingNames.OneDriveClearTokenCacheRequested, false)
                .Add<string, OneDriveCloudSupportExtension>(ExtensionSettingNames.OneDriveDriveAuthTokenCache, string.Empty);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
#if DEBUG
            builder.Add(ExtensionSettingNames.OneDriveClearTokenCacheRequested, new OneDriveAuthSettingUIPart());
#endif
        }
    }
}