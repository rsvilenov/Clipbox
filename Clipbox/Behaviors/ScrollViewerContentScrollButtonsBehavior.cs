using Clipbox.Infrastructure.UI.Utils;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// Controls the behavior of the scroll buttons when the scrollable content is wrapped in a ScrollViewer
    /// </summary>
    internal class ScrollViewerContentScrollButtonsBehavior : ScrollButtonBehavior
    {
        protected override ScrollViewer GetScrollViewer(FrameworkElement associatedScrollableElement)
        {
            return Traversing.FindParent<ScrollViewer>(associatedScrollableElement);
        }
    }
}
