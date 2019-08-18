using Clipbox.Infrastructure.UI.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// When the ListView scrolls to top/bottom, pass the input events to the containing element 
    /// </summary>
    public class ListViewScrollTransferFocusBehavior : Behavior<ListView>
    {
        private ScrollViewer _scrollViewer;

        private bool _areInputEventsWired;

        public FrameworkElement FocusRecieverElement
        {
            get { return (FrameworkElement)GetValue(FocusRecieverElementProperty); }
            set { SetValue(FocusRecieverElementProperty, value); }
        }

        public static readonly DependencyProperty FocusRecieverElementProperty =
            DependencyProperty.Register(nameof(FocusRecieverElement), typeof(FrameworkElement), typeof(ListViewScrollTransferFocusBehavior), new PropertyMetadata(null));

        
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
            //AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            _areInputEventsWired = true;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // the code below is not tested
            if ((e.Key == Key.Down && _scrollViewer.VerticalOffset == 0) ||
                ((e.Key == Key.Up || e.Key == Key.Tab) && _scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight))
            {
                e.Handled = true;

                // https://stackoverflow.com/questions/10820990/how-to-create-a-keyeventargs-object-in-wpf-related-to-a-so-answer
                var e2 = new KeyEventArgs(
                    Keyboard.PrimaryDevice,
                    Keyboard.PrimaryDevice.ActiveSource,
                    0, 
                    Key.Tab)
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent
                    };

                AssociatedObject.RaiseEvent(e2);
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
                //AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            }
        }
    }
}
