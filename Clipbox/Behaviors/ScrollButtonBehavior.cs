using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// Abstract class for behavors, which control the visibility and the operatoin of the scroll buttons. 
    /// Inherit this class, associate the inherited class as a behavior to the control, which has a scrollviewer.
    /// Implement GetScrollViewer in the subclasses - a method for finding the scrollviewer relative to the scrollable content.
    /// </summary>
    public abstract class ScrollButtonBehavior : Behavior<FrameworkElement>
    {
        private ScrollViewer _associatedScrollViewer;
        private bool _layoutUpdatedSubscribed;
        private bool _scrollBtnClickSubscribed;
        
        public ScrollDirection ButtonScrollDirection
        {
            get { return (ScrollDirection)GetValue(ButtonScrollDirectionProperty); }
            set { SetValue(ButtonScrollDirectionProperty, value); }
        }

        public static readonly DependencyProperty ButtonScrollDirectionProperty =
            DependencyProperty.Register(nameof(ButtonScrollDirection), typeof(ScrollDirection), typeof(ScrollButtonBehavior), new PropertyMetadata(ScrollDirection.Down));
        

        public Button ScrollButton
        {
            get { return (Button)GetValue(ScrollButtonProperty); }
            set { SetValue(ScrollButtonProperty, value); }
        }
        
        public static readonly DependencyProperty ScrollButtonProperty =
            DependencyProperty.Register(nameof(ScrollButton), typeof(Button), typeof(ScrollButtonBehavior), new PropertyMetadata(null, ScrollButtonProperty_PropertyChanged));

        private static void ScrollButtonProperty_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ((ScrollButtonBehavior)d).WireUpScrollButton();
            }
        }

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;

            WireUpAssociatedListView();

            if (AssociatedObject.IsLoaded && ScrollButton != null)
            {
                WireUpScrollButton();
            }
        }

        protected abstract ScrollViewer GetScrollViewer(FrameworkElement associatedScrollableElement);

        private void ScrollButton_Click(object sender, RoutedEventArgs e)
        {
            if (_associatedScrollViewer != null)
            {
                switch (ButtonScrollDirection)
                {
                    case ScrollDirection.Up:
                        _associatedScrollViewer.PageUp();
                        break;
                    case ScrollDirection.Down:
                        _associatedScrollViewer.PageDown();
                        break;
                    case ScrollDirection.Left:
                        _associatedScrollViewer.PageLeft();
                        break;
                    case ScrollDirection.Right:
                        _associatedScrollViewer.PageRight();
                        break;
                }
            }
        }

        private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WireUpAssociatedListView();

            if (ScrollButton != null)
            {
                WireUpScrollButton();
            }

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }

        private void WireUpScrollButton()
        {
            if (_scrollBtnClickSubscribed)
            {
                ScrollButton.Click -= ScrollButton_Click;
                _scrollBtnClickSubscribed = false;
            }

            ScrollButton.Click += ScrollButton_Click;
            _scrollBtnClickSubscribed = true;
        }

        private void WireUpAssociatedListView()
        {
            if (_associatedScrollViewer == null)
            {
                _associatedScrollViewer = GetScrollViewer(AssociatedObject);
                if (_associatedScrollViewer != null)
                {
                    _associatedScrollViewer.LayoutUpdated += _associatedScrollViewer_LayoutUpdated;
                    _layoutUpdatedSubscribed = true;
                }
            }
        }

        private void _associatedScrollViewer_LayoutUpdated(object sender, EventArgs e)
        {
            if (_associatedScrollViewer != null && ScrollButton != null)
            {
                switch (ButtonScrollDirection)
                {
                    case ScrollDirection.Up:
                        ScrollButton.Visibility = _associatedScrollViewer.ContentVerticalOffset > 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                        break;
                    case ScrollDirection.Down:
                        ScrollButton.Visibility = _associatedScrollViewer.ContentVerticalOffset < _associatedScrollViewer.ScrollableHeight ?
                        Visibility.Visible : Visibility.Collapsed;
                        break;
                    case ScrollDirection.Left:
                        ScrollButton.Visibility = _associatedScrollViewer.ContentHorizontalOffset > 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                        break;
                    case ScrollDirection.Right:
                        ScrollButton.Visibility = _associatedScrollViewer.ContentHorizontalOffset < _associatedScrollViewer.ScrollableWidth ?
                        Visibility.Visible : Visibility.Collapsed;
                        break;
                }
            }
        }

        protected override void OnDetaching()
        {
            if (_layoutUpdatedSubscribed)
            {
                _associatedScrollViewer.LayoutUpdated -= _associatedScrollViewer_LayoutUpdated;
            }

            if (_scrollBtnClickSubscribed)
            {
                ScrollButton.Click -= ScrollButton_Click;
            }
        }
        
        public enum ScrollDirection
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}
