using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Infrastructure.ExtentionMethods;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Clipbox.Extentions.BuiltIn.ImageFormats.FormatSupport
{
    [ClipboxExtention(Author = "Clipbox", Description = "", Version = 0.1)]
    [ClipboardFormat("CF_BITMAP_CRASHES")]
    public class BitmapFormatSupport 
        : ClipboardFormatSupport, IPersistableFormat
    {

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        IntPtr _hBitmap;

        public bool RequiresSameMachine
        {
            get
            {
                return false;
            }
        }

        public object ByteArrayToObject(byte[] byteArray)
        {
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                return new Bitmap(ms);
            }
        }

        public override IntPtr DataToPtr(ref object data)
        {
            if (!(data is Bitmap))
            {
                throw new ArgumentException("data should be Bitmap");
            }

            _hBitmap = (data as Bitmap).GetHbitmap();
            return _hBitmap;

            //using (var bmp = (Bitmap)(data as Bitmap).Clone())
            //{
            //    _hBitmap = bmp.GetHbitmap();
            //    return _hBitmap;
            //}
        }

        public byte[] ObjectToByteArray(ref object data)
        {
            var bmp = data as Bitmap;
            if (bmp != null)
            {
                return bmp.ToByteArray();
            }
            
            return null;
        }

        public override object PtrToData(IntPtr hClipboardData)
        {
            return Bitmap.FromHbitmap(hClipboardData);
        }

        /// <summary>
        /// Prevent GlobalFree (the default action on failure)
        /// from trying to free the hBitmap. This handle is
        /// taken care of in the Dispose method.
        /// </summary>
        /// <param name="hClipboardData"></param>
        public override void OnClipboardFailure(IntPtr hClipboardData)
        {
        }

        public override ClipboardContentClass ContentClass => ClipboardContentClass.Image;

        /// <summary>
        /// It looks like it is our responsibility to free the hBitmap
        /// after passing it to the clipboard
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            DeleteObject(_hBitmap);
        }
    }
}
