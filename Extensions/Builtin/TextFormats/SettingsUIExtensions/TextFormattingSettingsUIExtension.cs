using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.UIExtensions;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtension]
    public class TextFormattingSettingsUIExtension : SettingsUIExtension
    {
        private TextFormattingSettings _extensionUI;
        public override UIElement GetSettingsViewPart()
        {
            _extensionUI = new TextFormattingSettings
            {
                IsSaveSeparatorToggled = GetSetting<bool, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault)
            };

            _extensionUI.SettingChanged += _extensionUI_SettingChanged;

            return _extensionUI;
        }

        private void _extensionUI_SettingChanged(object sender, System.EventArgs e)
        {
            RequestSettingChange<bool, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, _extensionUI.IsSaveSeparatorToggled);
        }

        protected override void RegisterSettingKeys(SettingKeyCollectionBuilder settingKeyColBuilder)
        {
            settingKeyColBuilder
                .Add<int, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.DefaultSeparator, (int)TextConcatenateSeparators.None)
                .Add<bool, RemoveFormattingUIExtension>(ExtensionSettingNames.RemoveFormatting, false)
                .Add<bool, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.SaveSelectedSeparatorAsDefault, false)
                .Add<bool, RemoveFormattingUIExtension>(ExtensionSettingNames.SaveRemoveFormattingState, false);
        }
    }
}
