using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

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

        public static Bitmap Crop(this Bitmap src, Rectangle cropRect)
        {
            Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                 cropRect,
                                 GraphicsUnit.Pixel);
            }

            return target;
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

        public static Bitmap DeepClone(this Bitmap bmp)
        {
            return new Bitmap(bmp);
        }

    }
}
