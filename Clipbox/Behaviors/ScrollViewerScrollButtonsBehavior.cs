using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// Controls the behavior of the scroll buttons when the scrollable content is wrapped in a ScrollViewer
    /// </summary>
    internal class ScrollViewerScrollButtonsBehavior : ScrollButtonBehavior
    {
        protected override ScrollViewer GetScrollViewer(FrameworkElement associatedScrollableElement)
        {
            if (associatedScrollableElement is ScrollViewer)
            {
                return associatedScrollableElement as ScrollViewer;
            }
            else
            {
                throw new InvalidOperationException("This behavior can be attached only to an element of type ScrollViewer");
            }
        }
    }
}
