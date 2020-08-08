using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core;

namespace Clipbox.Extensions.BuiltIn.HdropFormat.FlowExtensions
{
    [ClipboxExtension(
        description: "Append numbers to the names of pasted files, which have the same names",
        author: ProductConstants.PRODUCT_NAME,
        version: 1.0,
        MinHostVersion = 1.0
        )]
    public class RenameExistingFilesFlowExtension : FlowExtension
    {
        public override void OnManagedPasteRequested(IClipboardItem clipboardItems, IWindowInfo targetWindowInfo, ref bool cancelPaste)
        {
            // resolve current explorer's address and if the folder contains files with names such as those awaiting paste, do what?
        }
    }
}
