using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class UnicodeTextIntermediateFormat : IIntermediateFormat
    {
        public string FormatGuid => "{CAF7A257-882B-46CE-A26D-679D228EA8B7}";

        public UnicodeTextIntermediateFormat(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public bool IsTransferable => true;

        public ClipboardContentClass ContentClass => ClipboardContentClass.Text;
    }
}
