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

        public bool IsScrollUpButton
        {
            get { return (bool)GetValue(ScrollUpProperty); }
            set { SetValue(ScrollUpProperty, value); }
        }

        public static readonly DependencyProperty ScrollUpProperty =
            DependencyProperty.Register("IsScrollUpButton", typeof(bool), typeof(ScrollButtonBehavior), new PropertyMetadata(false));



        public Button ScrollButton
        {
            get { return (Button)GetValue(ScrollButtonProperty); }
            set { SetValue(ScrollButtonProperty, value); }
        }
        
        public static readonly DependencyProperty ScrollButtonProperty =
            DependencyProperty.Register("ScrollButton", typeof(Button), typeof(ScrollButtonBehavior), new PropertyMetadata(null));



        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            if (AssociatedObject.IsLoaded)
            {
                WireUpAssociatedListView();
                WireUpScrollButton();
            }
        }

        protected abstract ScrollViewer GetScrollViewer(FrameworkElement associatedScrollableElement);

        private void ScrollButton_Click(object sender, RoutedEventArgs e)
        {
            if (_associatedScrollViewer != null)
            {
                if (IsScrollUpButton)
                {
                    _associatedScrollViewer.PageUp();
                }
                else
                {
                    _associatedScrollViewer.PageDown();
                }
            }
        }

        private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WireUpAssociatedListView();
            WireUpScrollButton();

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }

        private void WireUpScrollButton()
        {
            if (!_scrollBtnClickSubscribed)
            {
                ScrollButton.Click += ScrollButton_Click;
                _scrollBtnClickSubscribed = true;
            }
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
            if (_associatedScrollViewer != null)
            {
                if (IsScrollUpButton)
                {
                    ScrollButton.Visibility = _associatedScrollViewer.ContentVerticalOffset > 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    ScrollButton.Visibility = _associatedScrollViewer.ContentVerticalOffset < _associatedScrollViewer.ScrollableHeight ?
                    Visibility.Visible : Visibility.Collapsed;
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
    }
}
