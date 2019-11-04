using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;
using Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FlowExtentions
{
    [ClipboxExtention]
    public class ProcessModifyHdropTargets : FlowExtention
    {
        public override bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            // if all items have a FilesystemPathsIntermediateFormat in them, we can combine the targets...
            bool canCombine = items.All(item => item.ClipboardDataObjects.Any(cdo => cdo.IntermediateFormatObject is FilesystemPathsIntermediateFormat));
            return canCombine;
        }

        public override IGeneratedClipboardItem OnPerformModifyRequested(IEnumerable<IClipboardItem> items)
        {
            List<string> selectedPaths = new List<string>();

            List<string> multySelectionItemIds = new List<string>();
            Dictionary<string, int[]> multyItemsSelectionParameter = GetParameter<Dictionary<string, int[]>>("MultiItemSelection");
            if (multyItemsSelectionParameter != null)
            {
                foreach (string clipboardItemId in multyItemsSelectionParameter.Keys)
                {
                    var allPathsInItem = items.First(item => item.Id == clipboardItemId)
                        .ClipboardDataObjects.First(cdo => cdo.IntermediateFormatObject is FilesystemPathsIntermediateFormat)
                        .IntermediateFormatObject as FilesystemPathsIntermediateFormat;
                    foreach (int index in multyItemsSelectionParameter[clipboardItemId])
                    {
                        selectedPaths.Add(allPathsInItem.Paths[index]);
                    }
                    multySelectionItemIds.Add(clipboardItemId);
                }
            }
            
            // there shouldn't be more than one hdrop object in the list
            var hDropClipboardDataObjects = items
                .Where(item => !multySelectionItemIds.Contains(item.Id))
                .Select(item => item.ClipboardDataObjects.Single(cdo => cdo.IntermediateFormatObject is FilesystemPathsIntermediateFormat));

            // select all other format types and distinct by format type (i.e. Firefox puts image+file in the clipboard)
            var nonHdropClipboardDataObjects = items
                .Where(item => !multySelectionItemIds.Contains(item.Id))
                .SelectMany(item => item.ClipboardDataObjects.Where(cdo => cdo.IntermediateFormatObject is FilesystemPathsIntermediateFormat == false))
                .GroupBy(cdo => cdo.ClipboardFormatName)
                .Select(gr => gr.First());


            List<string> singleItemsSelectedPaths = hDropClipboardDataObjects.
                SelectMany(hd => new List<string>(((FilesystemPathsIntermediateFormat)hd.IntermediateFormatObject).Paths)).
                ToList();

            selectedPaths.AddRange(singleItemsSelectedPaths);
            
            DropEffects? requestedDropEffect = GetParameter<DropEffects?>(FlowParameters.PreferredDropEffect);
            DropEffects dropEffectToSet = DropEffects.DROPEFFECT_COPY;
            if (requestedDropEffect != null)
            {
                bool shouldChangeDropEffect = items
                    .SelectMany(item => item.ClipboardDataObjects)
                    .Where(cdo => cdo.IntermediateFormatObject is PreferredDropEffectIntermediateFormat)
                    .Any(cdo => ((PreferredDropEffectIntermediateFormat)cdo.IntermediateFormatObject).DropEffect != requestedDropEffect);
                if (shouldChangeDropEffect)
                {
                    dropEffectToSet = requestedDropEffect.Value;
                }
            }

            var generated = new GeneratedClipboardItem();
            generated.AddFormatAndIntermediateFormatObject("Preferred DropEffect", new PreferredDropEffectIntermediateFormat(dropEffectToSet));
            generated.AddFormatAndIntermediateFormatObject("CF_HDROP", new FilesystemPathsIntermediateFormat(selectedPaths.Distinct()));

            foreach (IClipboardDataObject cdo in nonHdropClipboardDataObjects)
            {
                generated.AddFormatAndIntermediateFormatObject(cdo.ClipboardFormatName, cdo.IntermediateFormatObject);
            }
            return generated;
        }
    }
}
