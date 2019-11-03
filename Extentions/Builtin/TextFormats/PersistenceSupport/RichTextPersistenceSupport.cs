using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PersistenceSupport
{
    [IntermediateFormatSupport("{D8F8C369-7F81-4138-9718-4F34EB1BD669}")]
    public class RichTextPersistenceSupport : PersistenceSupportExtension<RichTextIntermediateFormat>
    {
        protected override byte[] GetByteArray(RichTextIntermediateFormat intermediateFormat)
        {
            return Encoding.UTF8.GetBytes(intermediateFormat.Rtf);
        }

        protected override RichTextIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            return new RichTextIntermediateFormat(Encoding.UTF8.GetString(byteArray));
        }
    }
}
