using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;
using System;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension(
    description: "Setting UI extension for password skipping",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    public class SkipPasswordsSettingsUIExtension : SettingsUIExtension
    {
        internal const int DEFAULT_MIN_PASSWORD_LEN = 8;
        internal const int DEFAULT_MAX_PASSWORD_LEN = 20;

        protected override SettingsCategory Category => SettingsCategory.Security;

        private PasswordPatternsSettingsUIPart _patternUI;
        private PasswordLengthSettingUIPart _lengthUI;

        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            if (settingName == ExtensionSettingNames.SkipPasswords)
            {
                AdjustUIPartsVisibility();
            }
        }

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
            _patternUI = new PasswordPatternsSettingsUIPart();
            _lengthUI = new PasswordLengthSettingUIPart(DEFAULT_MIN_PASSWORD_LEN, DEFAULT_MAX_PASSWORD_LEN);

            AdjustUIPartsVisibility();

            builder
                .Add(ExtensionSettingNames.SkipPasswords, new SkipPasswordsSettingUIPart())
                .Add(ExtensionSettingNames.PasswordMatchOption, _patternUI)
                .Add(ExtensionSettingNames.PasswordLengthPair, _lengthUI);
        }

        private void AdjustUIPartsVisibility()
        {
            bool shouldOtherUIsBeVisible =
                                 GetSetting<bool, SkipPasswordsFlowExtension>
                                    (ExtensionSettingNames.SkipPasswords);

            _patternUI?.SetIsVisible(shouldOtherUIsBeVisible);
            _lengthUI?.SetIsVisible(shouldOtherUIsBeVisible);
        }

    }
}
