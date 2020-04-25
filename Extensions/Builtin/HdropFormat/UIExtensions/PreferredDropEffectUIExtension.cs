using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.UIExtensions
{
    [ClipboxExtensionAttribute]
    public class PreferredDropEffectUIExtension : UIExtension
    {
        PreferredDropEffect _extensionUI;

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.All(item => item.ClipboardDataObjects.Any(cdo => cdo.ClipboardFormatName == "CF_HDROP"));
        }

        public override void OnExtensionViewGenerating(IEnumerable<IClipboardItem> selectedItems, ref UIElement additionalViewElement, Size availableSize)
        {
            _extensionUI = new PreferredDropEffect();
            additionalViewElement = _extensionUI;

            IClipboardItem firstSelectedItem = selectedItems.First();
            PreferredDropEffectIntermediateFormat dropEffectIntermediateFormat = firstSelectedItem.ClipboardDataObjects
                .Select(cdo => cdo.IntermediateFormatObject)
                .FirstOrDefault(ifo => ifo is PreferredDropEffectIntermediateFormat) as PreferredDropEffectIntermediateFormat;

            bool isPreferredDropEffectCut = selectedItems.Count() == 1 
                && dropEffectIntermediateFormat?.DropEffect == Enums.DropEffects.DROPEFFECT_MOVE;

            _extensionUI.SetSelectedDropEffect(isPreferredDropEffectCut ?
                Enums.DropEffects.DROPEFFECT_MOVE :
                Enums.DropEffects.DROPEFFECT_COPY);

            _extensionUI.DropEffectSelectionChanged += _extensionUI_DropEffectSelectionChanged;
        }

        private void _extensionUI_DropEffectSelectionChanged(object sender, PreferredDropEffect.SelectedDropEffectEventArgs e)
        {
            SetParameter<Enums.DropEffects?>(FlowParameters.PreferredDropEffect, e.DropEffect);
        }
    }
}
