using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.Linq;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FlowExtentions
{
    [ClipboxExtention]
    public class CutFlowExtention : FlowExtention
    {
        public override void OnManagedPasteCompleted(IClipboardItem clipboardItem, IWindowInfo targetWindowInfo, ref bool removeItem)
        {
            if (!clipboardItem.ClipboardDataObjects.Any(obj => obj.Format == "CF_HDROP"))
            {
                return;
            }

            DropEffects? dropEffect = Common.GetDropEffectFromClipboardItem(clipboardItem);
            // if the requested operation is Cut, remove the item from the clipboard items list
            if (dropEffect.HasValue && dropEffect.Value.HasFlag(DropEffects.DROPEFFECT_MOVE))
            {
                removeItem = true;
            }
        }
        
    }
    
}
