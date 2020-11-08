using Clipbox.Core;
using System.Drawing;
using System.Reflection;

namespace Clipbox.Infrastructure.UI.Resources
{
    public static class ResourceUtils
    {
        public static Bitmap GetApplicationIconBitmap()
        {
            using (var iconStream = Assembly.GetAssembly(typeof(ResourceUtils))
                .GetManifestResourceStream("Clipbox.Infrastructure.UI.Resources.Images.Clip-WF.png"))
            {
                return (Bitmap)Image.FromStream(iconStream);
            }
        }

        public static Icon GetApplicationIcon(int width, int height)
        {
            using (Icon icon = Icon.ExtractAssociatedIcon(ProductConstants.MAIN_PROCESS_EXE_NAME))
            {
                return new Icon(icon, width, height);
            }
        }

    }
}
