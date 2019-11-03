using Clipbox.Extentions.BuiltIn.HdropFormat.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using System.Linq;
using Clipbox.Extentions.BuiltIn.HdropFormat.IntermediateFormats;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FlowExtentions
{
    [ClipboxExtention]
    public class CutFlowExtention : FlowExtention
    {
        public override void OnManagedPasteCompleted(IClipboardItem clipboardItem, IWindowInfo targetWindowInfo, ref bool removeItem)
        {
            PreferredDropEffectIntermediateFormat peIf = clipboardItem.ClipboardDataObjects
                .FirstOrDefault(cdo => cdo.IntermediateFormatObject is FilesystemPathsIntermediateFormat) 
                as PreferredDropEffectIntermediateFormat;

            if (peIf == null)
            {
                return;
            }
            
            // if the requested operation is Cut, remove the item from the clipboard items list
            if (peIf.DropEffect.HasFlag(DropEffects.DROPEFFECT_MOVE))
            {
                removeItem = true;
            }
        }
        
    }
    
}
