using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.Encryption;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{5E4E9C1F-A324-42A4-AE9E-AA64DD871CC8}")]
    public class HtmlPersistenceSupport : PersistenceSupportExtension<HtmlIntermediateFormat>
    {
        protected override byte[] GetByteArray(HtmlIntermediateFormat intermediateFormat)
        {
            byte[] htmlBytes = Encoding.UTF8.GetBytes(intermediateFormat.Html);
            return DpApiEncryption.Protect(htmlBytes);
        }

        protected override HtmlIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            byte[] decryptedBytes = DpApiEncryption.Unprotect(byteArray);
            return new HtmlIntermediateFormat(Encoding.UTF8.GetString(decryptedBytes));
        }
    }
}
