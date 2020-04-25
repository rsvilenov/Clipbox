using System.Drawing;
using System.Drawing.Imaging;

namespace Clipbox.Infrastructure.ExtensionMethods
{
    public static class MetafileExtensions
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
