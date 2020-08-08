using System;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Core;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension(
    description: "Setting UI extension for extensions, having to do with text formatting",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    public class TextFormattingSettingsUIExtension : SettingsUIExtension
    {
        private DefaultSeparatorSettingUIPart _defaultSeparatorUIPart;

        public override SettingsCategory Category => SettingsCategory.Pasting;

        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
                .Add<int, MultipleTextSelectionsAlignmentFlowUIExtension>(ExtensionSettingNames.DefaultSeparator, (int)TextConcatenateSeparators.None)
                .Add<bool, RemoveFormattingFlowUIExtension>(ExtensionSettingNames.RemoveFormatting, false)
                .Add<bool, MultipleTextSelectionsAlignmentFlowUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, false)
                .Add<bool, RemoveFormattingFlowUIExtension>(ExtensionSettingNames.SaveRemoveFormattingState, false);
        }

        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            if (settingName == ExtensionSettingNames.SaveSelectedSeparatorAsDefault)
            {
                AdjustUIPartsVisibility();
            }
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            _defaultSeparatorUIPart = new DefaultSeparatorSettingUIPart();
            _defaultSeparatorUIPart.SetIsVisible(!GetSetting<bool, MultipleTextSelectionsAlignmentFlowUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault));

            AdjustUIPartsVisibility();

            builder
                .Add(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, new SaveLastSeparatorSettingUIPart())
                .Add(ExtensionSettingNames.DefaultSeparator, _defaultSeparatorUIPart);
        }
        private void AdjustUIPartsVisibility()
        {
            bool shouldDefaultSeparatorUIBeVisible =
                                !GetSetting<bool, MultipleTextSelectionsAlignmentFlowUIExtension>
                                    (ExtensionSettingNames.SaveSelectedSeparatorAsDefault);

            _defaultSeparatorUIPart?.SetIsVisible(shouldDefaultSeparatorUIBeVisible);
        }
    }
}
