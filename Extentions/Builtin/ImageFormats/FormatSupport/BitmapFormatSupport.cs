using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Extentions.BuiltIn.ImageFormats.IntermediateFormats;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_BITMAP_CRASHES")]
    public class BitmapFormatSupport : ClipboardFormatSupport<BitmapIntermediateFormat>
    {
        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        IntPtr _hBitmap;
        
        /// <summary>
        /// Prevent GlobalFree (the default action on failure)
        /// from trying to free the hBitmap. This handle is
        /// taken care of in the Dispose method.
        /// </summary>
        /// <param name="hClipboardData"></param>
        public override void OnClipboardFailure(IntPtr hClipboardData)
        {
        }
        
        /// <summary>
        /// It looks like it is our responsibility to free the hBitmap
        /// after passing it to the clipboard
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DeleteObject(_hBitmap);
        }

        protected override BitmapIntermediateFormat GetIntermediateFormat(IntPtr hClipboardData)
        {
            using (Bitmap bmp = Bitmap.FromHbitmap(hClipboardData))
            {
                return new BitmapIntermediateFormat(bmp);
            }
        }

        protected override IntPtr GetPtr(BitmapIntermediateFormat intermediateFormat)
        {
            using (Bitmap bmp = intermediateFormat.GetCopyOfBitmap())
            {
                _hBitmap = bmp.GetHbitmap();
            }

            return _hBitmap;
        }
    }
}
