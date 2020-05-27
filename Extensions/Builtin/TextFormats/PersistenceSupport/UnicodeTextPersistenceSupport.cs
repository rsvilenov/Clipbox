using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using Clipbox.Infrastructure.Encryption;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextPersistenceSupport : PersistenceSupportExtension<UnicodeTextIntermediateFormat>
    {
        protected override byte[] GetByteArray(UnicodeTextIntermediateFormat intermediateFormat)
        {
            byte[] textBytes = Encoding.Unicode.GetBytes(intermediateFormat.Text);
            return DpApiEncryption.Protect(textBytes);
        }

        protected override UnicodeTextIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            byte[] decryptedBytes = DpApiEncryption.Unprotect(byteArray);
            return new UnicodeTextIntermediateFormat(Encoding.Unicode.GetString(byteArray));
        }
    }
}
