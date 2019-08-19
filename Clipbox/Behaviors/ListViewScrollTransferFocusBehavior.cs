using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// When the ListView scrolls to top/bottom, pass the input events to the containing element 
    /// </summary>
    public class ListViewScrollTransferFocusBehavior : Behavior<ListView>
    {
        private ScrollViewer _scrollViewer;

        private bool _areInputEventsWired;

        public event EventHandler<EventArgs> KeyboardFocusMovedOut;

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            if (AssociatedObject.IsLoaded)
            {
                Setup();
            }
        }

        private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            Setup();
        }

        private void Setup()
        {
            _scrollViewer = Traversing.GetVisualChildOfType<ScrollViewer>(AssociatedObject);
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            _areInputEventsWired = true;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            FocusNavigationDirection? focusDirection = null;

            switch (e.Key)
            {
                case Key.Right:
                    if (AssociatedObject.SelectedIndex == AssociatedObject.Items.Count - 1)
                    {
                        focusDirection = FocusNavigationDirection.Next;
                    }
                    break;
                case Key.Left:
                    if (AssociatedObject.SelectedIndex == 0)
                    {
                        focusDirection = FocusNavigationDirection.Previous;
                    }
                    break;
                case Key.Up:
                    if (_scrollViewer.VerticalOffset == 0)
                    {
                        focusDirection = FocusNavigationDirection.Previous;
                    }
                    break;
                case Key.Down:
                    if (_scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight)
                    {
                        focusDirection = FocusNavigationDirection.Next;
                    }
                    break;
            }

            if (focusDirection.HasValue)
            {
                e.Handled = true;

                // the code below is based on Sheridan's answer in the following thread:
                // https://stackoverflow.com/questions/29028263/wpf-move-focus-to-the-next-item-in-an-itemscontrol-on-enter

                // MoveFocus takes a TraveralReqest as its argument
                TraversalRequest request = new TraversalRequest(focusDirection.Value);

                // gets the element with keyboard focus
                UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

                // change keyboard focus
                if (elementWithFocus != null)
                {
                    elementWithFocus.MoveFocus(request);
                }

                KeyboardFocusMovedOut?.Invoke(this, new EventArgs());
            }
            
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // when the mouse wheel is scrolling up and we have reached the top
            // or when we are scrolling down and have reached the bottom
            // raise a new routed event with the mouse wheel info
            if ((e.Delta < 0 && _scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight)
                || e.Delta > 0 && _scrollViewer.VerticalOffset == 0)
            {
                // https://stackoverflow.com/questions/14348517/child-elements-of-scrollviewer-preventing-scrolling-with-mouse-wheel
                e.Handled = true;

                var e2 = new MouseWheelEventArgs(
                    e.MouseDevice,
                    e.Timestamp,
                    e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent
                };

                AssociatedObject.RaiseEvent(e2);
            }
        }
        

        protected override void OnDetaching()
        {
            if (_areInputEventsWired)
            {
                AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
                AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            }
        }
    }
}
