using System.Drawing;
using System.Reflection;

namespace Clipbox.Infrastructure.UI.Resources
{
    public static class ResourceUtils
    {
        public static Bitmap GetApplicationIcon()
        {
            using (var iconStream = Assembly.GetAssembly(typeof(ResourceUtils))
                .GetManifestResourceStream("Clipbox.Infrastructure.UI.Resources.Images.Clip-WF.png"))
            {
                return (Bitmap)Image.FromStream(iconStream);
            }
        }
    }
}
