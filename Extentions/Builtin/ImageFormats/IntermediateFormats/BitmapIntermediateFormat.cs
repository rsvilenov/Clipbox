using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Drawing;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats
{
    public class BitmapIntermediateFormat : IntermediateFormatBase
    {
        /// <summary>
        /// This constructor copies the bitmap data, so the passed bitmap
        /// reference can be disposed of safely.
        /// </summary>
        /// <param name="bitmap"></param>
        public BitmapIntermediateFormat(Bitmap bitmap)
        {
            _bitmap = new Bitmap(bitmap);
        }

        public Bitmap GetCopyOfBitmap()
        {
            if (_bitmap == null)
            {
                throw new ObjectDisposedException(nameof(BitmapIntermediateFormat));
            }

            return new Bitmap(_bitmap);
        }

        protected override void Dispose(bool disposing)
        {
            if (_bitmap != null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
        }

        private Bitmap _bitmap;

        protected override IntermediateFormatMetadata CreateMetadata()
        {
            return new IntermediateFormatMetadata(
                formatGuid: "{4422E0FD-3DD7-4B10-B167-DB63BB97D6E1}",
                contentClass: ClipboardContentClass.Image,
                isTransferable: false);
        }
    }
}
