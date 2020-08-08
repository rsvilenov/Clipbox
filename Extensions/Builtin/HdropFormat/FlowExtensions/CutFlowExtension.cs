using Clipbox.Extensions.BuiltIn.HdropFormat.Enums;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using System.Linq;
using Clipbox.Extensions.BuiltIn.HdropFormat.IntermediateFormats;
using Clipbox.Core;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FlowExtensions
{
    [ClipboxExtension(
        description: "Remove the files that has been cut-pasted from the clipboard storage",
        author: ProductConstants.PRODUCT_NAME,
        version: 1.0,
        MinHostVersion = 1.0
        )]
    public class CutFlowExtension : FlowExtension
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
