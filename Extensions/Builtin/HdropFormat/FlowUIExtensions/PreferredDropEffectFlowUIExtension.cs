using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.HdropFormat.FlowUIExtensions.UIControls;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FlowUIExtensions
{
    [ClipboxExtension(
        description: "UI part (buttons) for changing the preferred drop effect of files before pasting them",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
        )]
    public class PreferredDropEffectFlowUIExtension : FlowUIExtension<PreferredDropEffect>
    {
        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.All(item => item.ClipboardDataObjects.Any(cdo => cdo.ClipboardFormatName == "CF_HDROP"));
        }

        private void _extensionUI_DropEffectSelectionChanged(object sender, PreferredDropEffect.SelectedDropEffectEventArgs e)
        {
            SetParameter<Enums.DropEffects?>(FlowParameters.PreferredDropEffect, e.DropEffect);
        }

        protected override void OnExtensionUICreated(PreferredDropEffect extensionUIControl, IEnumerable<IClipboardItem> selectedItems, double maxWidth, double maxHeigt)
        {
            IClipboardItem firstSelectedItem = selectedItems.First();
            PreferredDropEffectIntermediateFormat dropEffectIntermediateFormat = firstSelectedItem.ClipboardDataObjects
                .Select(cdo => cdo.IntermediateFormatObject)
                .FirstOrDefault(ifo => ifo is PreferredDropEffectIntermediateFormat) as PreferredDropEffectIntermediateFormat;

            bool isPreferredDropEffectCut = selectedItems.Count() == 1
                && dropEffectIntermediateFormat?.DropEffect == Enums.DropEffects.DROPEFFECT_MOVE;

            extensionUIControl.SetSelectedDropEffect(isPreferredDropEffectCut ?
                Enums.DropEffects.DROPEFFECT_MOVE :
                Enums.DropEffects.DROPEFFECT_COPY);

            extensionUIControl.DropEffectSelectionChanged += _extensionUI_DropEffectSelectionChanged;
        }
    }
}
