using Clipbox.Win32.Native;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipbox.Infrastructure.UI.ExtensionMethods
{
    public static class BitmapSourceExtensions
    {
        public static Bitmap ToBitmap(this BitmapSource source)
        {
            Bitmap bitmap;
            using (var outStream = source.GetMemoryStream())
            {
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }


        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            BitmapSource bitSrc = null;
            var hBitmap = bitmap.GetHbitmap();

            try
            {
                bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero, Int32Rect.Empty,
                                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                Gdi32.DeleteObject(hBitmap);
            }

            bitSrc.Freeze();

            return bitSrc;
        }

        public static MemoryStream GetMemoryStream(this BitmapSource source)
        {
            MemoryStream stream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(source));
            enc.Save(stream);

            return stream;
        }

        public static byte[] ToByteArray(this BitmapSource source)
        {
            using (MemoryStream ms = source.GetMemoryStream())
            {
                return ms.ToArray();
            }
        }
    }
}
