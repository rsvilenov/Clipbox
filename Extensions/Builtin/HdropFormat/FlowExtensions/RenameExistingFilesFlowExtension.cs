using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FlowExtensions
{
    [ClipboxExtensionAttribute]
    public class RenameExistingFilesFlowExtension : FlowExtension
    {
        public override void OnManagedPasteRequested(IClipboardItem clipboardItems, IWindowInfo targetWindowInfo, ref bool cancelPaste)
        {
            // resolve current explorer's address and if the folder contains files with names such as those awaiting paste, do what?
        }
    }
}
