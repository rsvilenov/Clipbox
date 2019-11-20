using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class TextIntermediateFormat : IntermediateFormatBase
    {
        public TextIntermediateFormat(string text)
        {
            Text = text;
        }

        public string Text { get; }
        
        protected override IntermediateFormatMetadata CreateMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}",
                contentClass: ClipboardContentClass.Text,
                isTransferable: true);
        }
    }
}
