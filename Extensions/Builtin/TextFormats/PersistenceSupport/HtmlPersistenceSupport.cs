using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(
        description: "Persistence support for HTML data",
        author: ProductConstants.PRODUCT_NAME,
        version: "1.0",
        minHostVersion: "1.0"
    )]
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
