using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class SkipPasswordsSettingsUIExtension : SettingsUIExtension
    {
        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
                .Add<bool, SkipPasswordsFlowExtension>(ExtensionSettingNames.SkipPasswords, false)
                .Add<int, SkipPasswordsFlowExtension>(ExtensionSettingNames.PasswordMatchOption, (int)PasswordMatchOptions.Second);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder
                .Add(ExtensionSettingNames.SkipPasswords, new SkipPasswordsSettingUIPart())
                .Add(ExtensionSettingNames.PasswordMatchOption, new PasswordPatternsSettingsUIPart());
        }
    }
}
