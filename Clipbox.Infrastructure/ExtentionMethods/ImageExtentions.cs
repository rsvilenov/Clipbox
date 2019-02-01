using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.ExtentionMethods
{
    public static class ImageExtentions
    {
        public static Bitmap EnsureBitmap(this Image img)
        {
            Bitmap bmp;

            if (img is Metafile)
            {
                bmp = (img as Metafile).ToBitmap();
            }
            else
            {
                bmp = (Bitmap)img;
            }

            return bmp;
        }

        public static Image GetScaled(this Image image, int maxWidth, int maxHeight, System.Drawing.Drawing2D.InterpolationMode interpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var g = Graphics.FromImage(newImage))
            {
                g.InterpolationMode = interpolationMode;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

        public static Image GetScaled(this Image image, int maxHeight, System.Drawing.Drawing2D.InterpolationMode interpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor)
        {
            var ratio = (double)maxHeight / image.Height;

            var newWidth = (int)(image.Width * ratio);
            var newHeight = maxHeight;

            var newImage = new Bitmap(newWidth, newHeight);

            using (var g = Graphics.FromImage(newImage))
            {
                g.InterpolationMode = interpolationMode;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

        public static Image GetScaled(this Image image, int width, int height)
        {
            double dbl = (double)image.Width / (double)image.Height;

            Image scaled = null;

            if ((int)((double)height * dbl) <= width)
            {
                scaled = new Bitmap(image, (int)((double)height * dbl), height);
            }
            else
            {
                scaled = new Bitmap(image, width, (int)((double)width / dbl));
            }

            return scaled;
        }
    }
}
