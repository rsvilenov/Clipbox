using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PersistenceTypes;
using Clipbox.Extensions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtensionMethods;
using System.Drawing;
using System.IO;

namespace Clipbox.Extensions.BuiltIn.ImageFormats.PersistenceSupport
{
    [ClipboxExtension(
    description: "Persistence support for bitmap images",
    author: ProductConstants.PRODUCT_NAME,
    version: 1.0,
    MinHostVersion = 1.0
    )]
    [IntermediateFormatSupport("{4422E0FD-3DD7-4B10-B167-DB63BB97D6E1}")]
    public class BitmapPersistenceSupport : PersistenceSupportExtension<BitmapIntermediateFormat>
    {
        protected override byte[] GetByteArray(BitmapIntermediateFormat intermediateFormat)
        {
            return intermediateFormat.GetCopyOfBitmap().ToByteArray();
        }

        protected override BitmapIntermediateFormat GetIntermediateFormat(byte[] byteArray)
        {
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                using (Bitmap bmp = new Bitmap(ms))
                {
                    return new BitmapIntermediateFormat(bmp);
                }
            }
        }
    }
}
