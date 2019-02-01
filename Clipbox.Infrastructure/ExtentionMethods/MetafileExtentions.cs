using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipbox.Infrastructure.ExtentionMethods
{
    public static class MetafileExtentions
    {
        public static BitmapSource ToBitmapSource(this Metafile metafile)
        {
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(metafile.Width, metafile.Height))
            {
                bmp.SetResolution(metafile.HorizontalResolution, metafile.VerticalResolution);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.DrawImage(metafile, 0, 0);
                    BitmapSource bms = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bms.Freeze();
                    return bms;
                }
            }
        }

        public static Bitmap ToBitmap(this Metafile metafile)
        {
            Bitmap result = new Bitmap(metafile.Width, metafile.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(metafile, 0, 0, metafile.Width, metafile.Height);
            }
            return result;
        }
    }
}
