using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.PersistenceSupport
{
    [ClipboxExtension(
    description: "Persistence support for Dib V5 images",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [IntermediateFormatSupport("{CFCA3819-C2EF-4F94-B9B7-49D402CA0652}")]
    public class DibV5PersistenceSupport : PersistenceSupportExtension<DibV5IntermediateFormat>
    {
        protected override byte[] GetByteArray(DibV5IntermediateFormat intermediateFormat)
        {
            return intermediateFormat.GetDibV5Bytes();
        }

        protected override DibV5IntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            return new DibV5IntermediateFormat(byteArray);
        }
    }
}
