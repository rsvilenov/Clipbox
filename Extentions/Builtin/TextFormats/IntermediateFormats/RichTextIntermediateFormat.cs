using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class RichTextIntermediateFormat : IntermediateFormatBase
    {
        public RichTextIntermediateFormat(string rtf)
        {
            Rtf = rtf;
        }

        public string Rtf { get; }
        
        protected override IntermediateFormatMetadata DefineMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{D8F8C369-7F81-4138-9718-4F34EB1BD669}",
                contentClass: ClipboardContentClass.RichText,
                isTransferable: true);
        }
    }
}
