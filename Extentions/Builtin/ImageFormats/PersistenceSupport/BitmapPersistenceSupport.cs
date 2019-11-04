using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.PersistenceTypes;
using Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats;
using Clipbox.Infrastructure.ExtentionMethods;
using System.Drawing;
using System.IO;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.PersistenceSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
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
