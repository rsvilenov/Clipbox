using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.FlowExtentions
{
    [ClipboxExtention]
    public class RenameExistingFilesFlowExtention : FlowExtention
    {
        public override void OnPasteRequested(IClipboardItem clipboardItems, IWindowInfo targetWindowInfo, ref bool cancelPaste)
        {
            // resolve current explorer's address and if the folder contains files with names such as those awaiting paste, do what?
        }
    }
}
