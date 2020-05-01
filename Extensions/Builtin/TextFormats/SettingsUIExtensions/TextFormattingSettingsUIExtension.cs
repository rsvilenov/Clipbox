using System;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts;
using Clipbox.Extensions.BuiltIn.TextFormats.UIExtensions;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class TextFormattingSettingsUIExtension : SettingsUIExtension
    {
        private DefaultSeparatorSettingUIPart _defaultSeparatorUIPart;
        
        protected override void RegisterSettings(IRegisteredSettingStore settingsStore)
        {
            settingsStore
                .Add<int, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.DefaultSeparator, (int)TextConcatenateSeparators.None)
                .Add<bool, RemoveFormattingUIExtension>(ExtensionSettingNames.RemoveFormatting, false)
                .Add<bool, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, false)
                .Add<bool, RemoveFormattingUIExtension>(ExtensionSettingNames.SaveRemoveFormattingState, false);
        }

        protected override void OnSettingUpdated(string settingName, Type targetType)
        {
            bool shouldDefaultSeparatorUIBeVisible = 
                !(settingName == ExtensionSettingNames.SaveSelectedSeparatorAsDefault
                && GetSetting<bool, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault));
            
            _defaultSeparatorUIPart?.SetIsVisible(shouldDefaultSeparatorUIBeVisible);
        }

        protected override void RegisterSettingUIParts(ISettingsUIBuilder builder)
        {
            _defaultSeparatorUIPart = new DefaultSeparatorSettingUIPart();
            _defaultSeparatorUIPart.SetIsVisible(!GetSetting<bool, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault));

            builder
                .Add(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, new SaveLastSeparatorSettingUIPart())
                .Add(ExtensionSettingNames.DefaultSeparator, _defaultSeparatorUIPart);
        }
    }
}
