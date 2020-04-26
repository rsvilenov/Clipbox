using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;

namespace Clipbox.Extensions.BuiltIn.TextFormats.UIExtensions
{
    [ClipboxExtensionAttribute]
    public class RemoveFormattingUIExtension : UIExtension
    {
        private const string REMOVE_FORMATING_SETTING_KEY = nameof(REMOVE_FORMATING_SETTING_KEY);

        private RemoveFormatting _extensionUI;
        
        internal void ToggleRemoveFormatting(bool isOn)
        {
            SetParameter(FlowParameters.RemoveFormatting, isOn);
        }

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() >= 1 
                && concatinatedItems.All(item => item.ClipboardDataObjects.Any(obj => obj.IntermediateFormatObject is RichTextIntermediateFormat));
        }

        public override void OnExtensionViewGenerating(IEnumerable<IClipboardItem> concatinatedItems, ref UIElement additionalViewElement, Size availableSize)
        {
            _extensionUI = new RemoveFormatting();
            _extensionUI.RemoveFormattingChanged += _extensionUI_RemoveFormattingChanged;
            bool toggled = GetSetting<bool>(ExtensionSettingNames.RemoveFormatting);

            _extensionUI.ToggleRemoveFormatting(toggled);

            ToggleRemoveFormatting(toggled);

            additionalViewElement = _extensionUI;
        }

        private void _extensionUI_RemoveFormattingChanged(object sender, RemoveFormatting.RemoveFormattingEventArgs e)
        {
            ToggleRemoveFormatting(e.Toggled);
            RequestSettingChange(ExtensionSettingNames.RemoveFormatting, e.Toggled);
        }
    }
}
