using Clipbox.Win32.Native;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Clipbox.Infrastructure.ExtentionMethods
{
    public static class BitmapExtentions
    {
        public static byte[] ToByteArray(this Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
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

        public static Icon ToIcon(this Bitmap bitmap)
        {
            IntPtr hIcon = bitmap.GetHicon();
            try
            {
                return Icon.FromHandle(hIcon);
            }
            finally
            {
                //User32.DestroyIcon(hIcon);
            }
        }

        public static Bitmap GetResized(this Bitmap src, int width, int height, System.Drawing.Drawing2D.InterpolationMode interpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = interpolationMode;
                g.DrawImage(src, 0, 0, width, height);
            }
            return result;
        }
        
        public static Bitmap SuperimposeBitmap(this Bitmap largeBmp, Bitmap smallBmp)
        {
            using (Graphics g = Graphics.FromImage(largeBmp))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                //int x = largeBmp.Width - smallBmp.Width;
                //int y = largeBmp.Height - smallBmp.Height;
                g.DrawImage(smallBmp, new System.Drawing.Point(0, 0));
            }
            return largeBmp;
        }

        public static Bitmap GetCenteredOverTransparentCanvas(this Bitmap image, int canvasWidth, int canvasHeight)
        {
            Bitmap canvas = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppArgb);

            Image sourceImage = null;
            if (false && image.Width > canvasWidth || image.Height > canvasHeight)
            {
                sourceImage = image.GetScaled(canvasWidth, canvasHeight);
            }
            else
            {
                sourceImage = image;
            }

            int x = (canvasWidth - sourceImage.Width) / 2;
            int y = (canvasHeight - sourceImage.Height) / 2;

            using (Graphics canvasGr = Graphics.FromImage(canvas))
            {
                canvasGr.DrawImage(sourceImage, x, y, sourceImage.Width, sourceImage.Height);
            }

            return canvas;
        }

    }
}
