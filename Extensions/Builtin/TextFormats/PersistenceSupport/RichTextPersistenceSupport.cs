using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(Author = "Clipbox", Description = "", Version = 0.1)]
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
