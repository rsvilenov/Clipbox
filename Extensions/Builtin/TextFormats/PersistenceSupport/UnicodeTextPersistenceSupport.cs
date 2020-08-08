using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(
    description: "Persistence support for Unicode text",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [IntermediateFormatSupport("{CAF7A257-882B-46CE-A26D-679D228EA8B7}")]
    public class UnicodeTextPersistenceSupport : PersistenceSupportExtension<UnicodeTextIntermediateFormat>
    {
        protected override byte[] GetByteArray(UnicodeTextIntermediateFormat intermediateFormat)
        {
            return Encoding.Unicode.GetBytes(intermediateFormat.Text);
        }

        protected override UnicodeTextIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            return new UnicodeTextIntermediateFormat(Encoding.Unicode.GetString(byteArray));
        }

        protected override bool ShouldEncryptData => true;
    }
}
