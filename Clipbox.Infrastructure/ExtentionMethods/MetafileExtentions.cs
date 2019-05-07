using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Infrastructure.ExtentionMethods
{
    public static class MetafileExtentions
    {
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
