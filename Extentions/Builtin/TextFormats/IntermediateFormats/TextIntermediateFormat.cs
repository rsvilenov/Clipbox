using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class TextIntermediateFormat : IIntermediateFormat
    {
        public string FormatGuid => "{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}";

        public TextIntermediateFormat(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public bool IsTransferable => true;

        public ClipboardContentClass ContentClass => ClipboardContentClass.Text;
    }
}
