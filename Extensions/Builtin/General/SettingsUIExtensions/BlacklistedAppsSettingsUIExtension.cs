using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Extensions.BuiltIn.General.FlowExtensions;
using Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions
{
    [ClipboxExtension]
    public class BlacklistedAppsSettingsUIExtension : SettingsUIExtension
    {
        public override SettingsCategory Category => SettingsCategory.Copying;

        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore.Add<string, CancelCopyOfBlacklistedAppsFlowExtension>(ExtensionSettingNames.BlacklistedApps, string.Empty);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder.Add(ExtensionSettingNames.BlacklistedApps, new BlacklistedAppsSettingUIPart());
        }
    }
}
