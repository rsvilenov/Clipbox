using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PersistenceSupport
{
    [IntermediateFormatSupport("{5E4E9C1F-A324-42A4-AE9E-AA64DD871CC8}")]
    public class HtmlPersistenceSupport : PersistenceSupportExtension<HtmlIntermediateFormat>
    {
        protected override byte[] GetByteArray(HtmlIntermediateFormat intermediateFormat)
        {
            return Encoding.UTF8.GetBytes(intermediateFormat.Html);
        }

        protected override HtmlIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            return new HtmlIntermediateFormat(Encoding.UTF8.GetString(byteArray));
        }
    }
}
