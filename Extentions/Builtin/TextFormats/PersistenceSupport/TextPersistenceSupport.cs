using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extentions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [IntermediateFormatSupport("{89BCFB06-B1FC-4C0D-BFE6-9643BED5A172}")]
    public class TextPersistenceSupport : PersistenceSupportExtension<TextIntermediateFormat>
    {
        protected override byte[] GetByteArray(TextIntermediateFormat intermediateFormat)
        {
            return Encoding.UTF8.GetBytes(intermediateFormat.Text);
        }

        protected override TextIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            return new TextIntermediateFormat(Encoding.UTF8.GetString(byteArray));
        }
    }
}
