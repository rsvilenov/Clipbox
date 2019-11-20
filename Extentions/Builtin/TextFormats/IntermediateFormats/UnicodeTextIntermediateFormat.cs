using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class UnicodeTextIntermediateFormat : IntermediateFormatBase
    {
        public UnicodeTextIntermediateFormat(string text)
        {
            Text = text;
        }

        public string Text { get; }
        
        protected override IntermediateFormatMetadata CreateMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{CAF7A257-882B-46CE-A26D-679D228EA8B7}",
                contentClass: ClipboardContentClass.Text,
                isTransferable: true);
        }
    }
}
