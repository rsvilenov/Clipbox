using Clipbox.Infrastructure.UI.Utils;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Behaviors
{
    public class ListViewScrollButtonBehavior : ScrollButtonBehavior
    {
        protected override ScrollViewer GetScrollViewer(FrameworkElement associatedScrollableElement)
        {
            return Traversing.GetVisualChildOfType<ScrollViewer>(associatedScrollableElement);
        }
    }
}
