using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FlowExtentions
{
    [ClipboxExtention]
    public class ProcessModifyHdropTargets : FlowExtention
    {
        public override bool OnQueryModify(IEnumerable<IClipboardItem> items)
        {
            // if all items have a CD_HDROP clipboard item in them, we can combine the targets...
            bool canCombine = items.All(item => item.ClipboardDataObjects.Any(cdo => cdo.Format == "CF_HDROP"));
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
                    var allPathsInItem = items.First(item => item.Id == clipboardItemId).ClipboardDataObjects.Single(cdo => cdo.Format == "CF_HDROP").OriginalData as string[];
                    foreach (int index in multyItemsSelectionParameter[clipboardItemId])
                    {
                        selectedPaths.Add(allPathsInItem[index]);
                    }
                    multySelectionItemIds.Add(clipboardItemId);
                }
            }
            
            // there shouldn't be more than one hdrop object in the list
            var hDropClipboardDataObjects = items
                .Where(item => !multySelectionItemIds.Contains(item.Id))
                .Select(item => item.ClipboardDataObjects.Single(cdo => cdo.Format == "CF_HDROP"));

            // select all other format types and distinct by format type (i.e. Firefox puts image+file in the clipboard)
            var nonHdropClipboardDataObjects = items
                .Where(item => !multySelectionItemIds.Contains(item.Id))
                .SelectMany(item => item.ClipboardDataObjects.Where(cdo => cdo.Format != "CF_HDROP"))
                .GroupBy(cdo => cdo.Format)
                .Select(gr => gr.First());


            List<string> singleItemsSelectedPaths = hDropClipboardDataObjects.
                SelectMany(hd => new List<string>(hd.OriginalData as string[])).
                ToList();

            selectedPaths.AddRange(singleItemsSelectedPaths);
            
            DropEffects? requestedDropEffect = GetParameter<DropEffects?>(FlowParameters.PreferredDropEffect);
            DropEffects dropEffectToSet = DropEffects.DROPEFFECT_COPY;
            if (requestedDropEffect != null)
            {
                bool shouldChangeDropEffect = items.Select(item => Common.GetDropEffectFromClipboardItem(item)).Any(de => de != requestedDropEffect);
                if (shouldChangeDropEffect)
                {
                    dropEffectToSet = requestedDropEffect.Value;
                }
            }

            var generated = new GeneratedClipboardItem();
            generated.AddFormatAndData("Preferred DropEffect", (uint)dropEffectToSet, ClipboardContentClass.AuxilliaryData); 
            generated.AddFormatAndData("CF_HDROP", selectedPaths.Distinct().ToArray(), ClipboardContentClass.File);
            foreach (IClipboardDataObject cdo in nonHdropClipboardDataObjects)
            {
                generated.AddFormatAndData(cdo.Format, cdo.OriginalData);
            }
            return generated;
        }
    }
}
