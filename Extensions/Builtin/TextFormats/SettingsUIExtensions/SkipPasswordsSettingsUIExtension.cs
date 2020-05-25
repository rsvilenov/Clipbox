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
        internal const int DEFAULT_MIN_PASSWORD_LEN = 8;
        internal const int DEFAULT_MAX_PASSWORD_LEN = 20;

        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
                .Add<bool, SkipPasswordsFlowExtension>(ExtensionSettingNames.SkipPasswords, false)
                .Add<int, SkipPasswordsFlowExtension>(ExtensionSettingNames.PasswordMatchOption, (int)PasswordMatchOptions.Second)
                .Add<string, SkipPasswordsFlowExtension>(ExtensionSettingNames.PasswordLengthPair, 
                    $"{DEFAULT_MIN_PASSWORD_LEN}:{DEFAULT_MAX_PASSWORD_LEN}");
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            builder
                .Add(ExtensionSettingNames.SkipPasswords, new SkipPasswordsSettingUIPart())
                .Add(ExtensionSettingNames.PasswordMatchOption, new PasswordPatternsSettingsUIPart())
                .Add(ExtensionSettingNames.PasswordLengthPair, new PasswordLengthSettingUIPart(DEFAULT_MIN_PASSWORD_LEN, DEFAULT_MAX_PASSWORD_LEN));
        }
    }
}
