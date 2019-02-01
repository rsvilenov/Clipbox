using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Clipbox.Infrastructure.UI.Utils
{
    public static class Layout
    {
        public static Size GetVisibleSize(FrameworkElement element)
        {
            Rect? bounds = LayoutInformation.GetLayoutClip(element)?.Bounds;
            if (bounds == null)
            {
                return new Size(element.ActualWidth, element.ActualHeight);
            }
            return new Size(bounds.Value.Width, bounds.Value.Height);
        }


        public static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            // https://stackoverflow.com/questions/1517743/in-wpf-how-can-i-determine-whether-a-control-is-visible-to-the-user

            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }
    }
}
