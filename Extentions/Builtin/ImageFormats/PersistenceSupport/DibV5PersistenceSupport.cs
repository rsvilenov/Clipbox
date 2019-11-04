using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.PersistenceSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
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
