using System;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions;
using Clipbox.Core.Extensibility.Enums;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
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
            bool shouldDefaultSeparatorUIBeVisible = 
                !(settingName == ExtensionSettingNames.SaveSelectedSeparatorAsDefault
                && GetSetting<bool, MultipleTextSelectionsAlignmentFlowUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault));
            
            _defaultSeparatorUIPart?.SetIsVisible(shouldDefaultSeparatorUIBeVisible);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            _defaultSeparatorUIPart = new DefaultSeparatorSettingUIPart();
            _defaultSeparatorUIPart.SetIsVisible(!GetSetting<bool, MultipleTextSelectionsAlignmentFlowUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault));

            builder
                .Add(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, new SaveLastSeparatorSettingUIPart())
                .Add(ExtensionSettingNames.DefaultSeparator, _defaultSeparatorUIPart);
        }
    }
}
