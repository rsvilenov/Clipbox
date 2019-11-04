using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
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
    }
}
