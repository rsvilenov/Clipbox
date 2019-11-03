using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.UIExtentions
{
    [ClipboxExtention]
    public class PreferredDropEffectUIExtention : UIExtention
    {
        PreferredDropEffect _extentionUI;

        public override bool OnShouldShow(IEnumerable<IClipboardItem> concatinatedItems)
        {
            return concatinatedItems.All(item => item.ClipboardDataObjects.Any(cdo => cdo.ClipboardFormatName == "CF_HDROP"));
        }

        public override void OnExtentionViewGenerating(IEnumerable<IClipboardItem> selectedItems, ref UIElement additionalViewElement, Size availableSize)
        {
            _extentionUI = new PreferredDropEffect();
            additionalViewElement = _extentionUI;

            IClipboardItem firstSelectedItem = selectedItems.First();
            PreferredDropEffectIntermediateFormat dropEffectIntermediateFormat = firstSelectedItem.ClipboardDataObjects
                .Select(cdo => cdo.IntermediateFormatObject)
                .FirstOrDefault(ifo => ifo is PreferredDropEffectIntermediateFormat) as PreferredDropEffectIntermediateFormat;

            bool isPreferredDropEffectCut = selectedItems.Count() == 1 
                && dropEffectIntermediateFormat.DropEffect == Enums.DropEffects.DROPEFFECT_MOVE;

            _extentionUI.SetSelectedDropEffect(isPreferredDropEffectCut ?
                Enums.DropEffects.DROPEFFECT_MOVE :
                Enums.DropEffects.DROPEFFECT_COPY);

            _extentionUI.DropEffectSelectionChanged += _extentionUI_DropEffectSelectionChanged;
        }

        private void _extentionUI_DropEffectSelectionChanged(object sender, PreferredDropEffect.SelectedDropEffectEventArgs e)
        {
            SetParameter<Enums.DropEffects?>(FlowParameters.PreferredDropEffect, e.DropEffect);
        }
    }
}
