using Clipbox.Infrastructure.UI.ExtensionMethods;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Clipbox.Infrastructure.UI.Utils
{
    public static class Imaging
    {
        public static BitmapSource BitmapSourceFromBitmapByteArray(byte[] bitmapByteArray)
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(bitmapByteArray))
            using (bmp = new Bitmap(ms))
            {
                BitmapSource bms = bmp.ToBitmapSource();
                bms.Freeze();
                return bms;
            }
        }
    }
}
