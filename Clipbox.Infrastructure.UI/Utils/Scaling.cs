using System.Windows;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Utils
{
    public static class Scaling
    {
        public static void ScaleElement(double scaleFactor, FrameworkElement child)
        {
            if (scaleFactor != 1.0)
            {
                var dpiScale = new ScaleTransform(scaleFactor, scaleFactor);
                //child.SetValue(Window.LayoutTransformProperty, null);
                child.SetValue(FrameworkElement.LayoutTransformProperty, dpiScale);
            }
            else
            {
                child.SetValue(FrameworkElement.LayoutTransformProperty, null);
            }
        }
    }
}
