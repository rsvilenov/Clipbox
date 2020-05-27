using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.Encryption;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    public class TextPersistenceSupport : PersistenceSupportExtension<TextIntermediateFormat>
    {
        protected override byte[] GetByteArray(TextIntermediateFormat intermediateFormat)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(intermediateFormat.Text);
            return DpApiEncryption.Protect(textBytes);
        }

        protected override TextIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            byte[] decryptedBytes = DpApiEncryption.Unprotect(byteArray);
            return new TextIntermediateFormat(Encoding.UTF8.GetString(decryptedBytes));
        }
    }
}
