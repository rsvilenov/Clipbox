using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class RichTextIntermediateFormat : IIntermediateFormat
    {
        public string FormatGuid => "{D8F8C369-7F81-4138-9718-4F34EB1BD669}";

        public RichTextIntermediateFormat(string rtf)
        {
            Rtf = rtf;
        }

        public string Rtf { get; }

        public bool IsTransferable => true;

        public ClipboardContentClass ContentClass => ClipboardContentClass.Text;
    }
}
