using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.TextFormats.IntermediateFormats;
using System.Text;

namespace Clipbox.Extensions.BuiltIn.TextFormats.PersistenceSupport
{
    [ClipboxExtension(
    description: "Persistence support for ANSI text",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
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

        protected override bool ShouldEncryptData => true;
    }
}
