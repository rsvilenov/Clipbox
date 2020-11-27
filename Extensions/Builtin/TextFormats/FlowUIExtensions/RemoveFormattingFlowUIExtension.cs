using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions.UIControls;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions
{
    [ClipboxExtension(
        description: "Setting UI controls for the extension that removes the formatting of text entries",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
    public class RemoveFormattingFlowUIExtension : FlowUIExtension<RemoveFormatting>
    {   
        internal void ToggleRemoveFormatting(bool isOn)
        {
            SetParameter(FlowParameters.RemoveFormatting, isOn);
        }

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.Count() == 1
                && concatinatedItems.Single().ClipboardDataObjects.
                    Any(cdo => cdo.IntermediateFormatMetadata.FormatGuid == "{D8F8C369-7F81-4138-9718-4F34EB1BD669}"); // RichTextIntermediateFormat
        }

        private void _extensionUI_RemoveFormattingChanged(object sender, RemoveFormatting.RemoveFormattingEventArgs e)
        {
            ToggleRemoveFormatting(e.Toggled);
            if (GetSetting<bool>(ExtensionSettingNames.SaveRemoveFormattingState))
            {
                RequestSettingChange(ExtensionSettingNames.RemoveFormatting, e.Toggled);
            }
        }

        protected override void OnExtensionUICreated(RemoveFormatting extensionUIControl, IEnumerable<IClipboardItem> selectedItems, double maxWidth, double maxHeigt)
        {
            extensionUIControl.RemoveFormattingChanged += _extensionUI_RemoveFormattingChanged;
            bool toggled = GetSetting<bool>(ExtensionSettingNames.RemoveFormatting);

            extensionUIControl.ToggleRemoveFormatting(toggled);

            ToggleRemoveFormatting(toggled);
        }
    }
}
