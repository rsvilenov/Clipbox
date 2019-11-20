using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats
{
    public class HtmlIntermediateFormat : IntermediateFormatBase
    {
        public HtmlIntermediateFormat(string html)
        {
            Html = html;
        }

        public string Html { get; }
        
        protected override IntermediateFormatMetadata CreateMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{5E4E9C1F-A324-42A4-AE9E-AA64DD871CC8}",
                contentClass: ClipboardContentClass.Text,
                isTransferable: true);
        }
    }
}
