using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class HtmlIntermediateFormat : IIntermediateFormat
    {
        public string FormatGuid => "{5E4E9C1F-A324-42A4-AE9E-AA64DD871CC8}";

        public HtmlIntermediateFormat(string html)
        {
            Html = html;
        }

        public string Html { get; }

        public bool IsTransferable => true;

        public ClipboardContentClass ContentClass => ClipboardContentClass.Text;
    
    }
}
