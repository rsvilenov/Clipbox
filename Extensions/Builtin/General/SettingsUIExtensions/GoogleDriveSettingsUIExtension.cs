using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Extensions.BuiltIn.General.CloudSupportExtensions;
using Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions
{
    [ClipboxExtension(
           description: "Settings UI for Google Drive cloud support",
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
                .Add<bool, GoogleDriveCloudSupportExtension>(ExtensionSettingNames.GoogleDriveReauthRequested, false)
                .Add<string, GoogleDriveCloudSupportExtension>(ExtensionSettingNames.GoogleDriveAuthToken, string.Empty);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder.Add(ExtensionSettingNames.GoogleDriveReauthRequested, new GoogleCloudAuthSettingUIPart());
        }
    }
}
