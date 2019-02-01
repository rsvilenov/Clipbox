using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Clipbox.Infrastructure.ExtentionMethods
{
    public static class BitmapSourceExtentions
    {
        public static Bitmap ToBitmap(this BitmapSource source)
        {
            Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(source));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

    }
}
