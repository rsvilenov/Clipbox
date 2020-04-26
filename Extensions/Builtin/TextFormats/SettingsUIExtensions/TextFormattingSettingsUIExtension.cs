using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowExtensions;
using Clipbox.Extensions.BuiltIn.TextFormats.UIExtensions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    [ClipboxExtensionAttribute]
    public class TextFormattingSettingsUIExtension : SettingsUIExtension
    {
        public override UIElement GetSettingsViewPart()
        {
            return new Border
            {
                Width = 200,
                Height = 50,
                Background = Brushes.Green
            };
        }

        protected override void RegisterSettingKeys(SettingKeyCollectionBuilder settingKeyColBuilder)
        {
            settingKeyColBuilder
                .Add<int, MultipleTextSelectionsAlignmentUIExtension>(ExtensionSettingNames.DefaultSeparator, (int)TextConcatenateSeparators.None)
                .Add<bool, RemoveFormattingUIExtension>(ExtensionSettingNames.RemoveFormatting, false);
        }
    }
}
