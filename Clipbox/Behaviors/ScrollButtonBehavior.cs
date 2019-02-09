using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    /// <summary>
    /// Abstract class for behavors, which control the visibility of the scroll buttons. 
    /// Inherit this class, associate the inherited class as a behavior to each of the
    /// scroll buttons and pass the scrollable content as the value for the Tag
    /// property of the scrollable button. Implement GetScrollViewer in the subclasses -  a method for
    /// finding the scrollviewer relative to the scrollable content.
    /// </summary>
    public abstract class ScrollButtonBehavior : Behavior<Button>
    {
        private FrameworkElement _associatedScrollableElement;
        private ScrollViewer _associatedScrollViewer;
        private bool _layoutUpdatedSubscribed;

        public bool IsScrollUpButton
        {
            get { return (bool)GetValue(ScrollUpProperty); }
            set { SetValue(ScrollUpProperty, value); }
        }

        public static readonly DependencyProperty ScrollUpProperty =
            DependencyProperty.Register("IsScrollUpButton", typeof(bool), typeof(ScrollButtonBehavior), new PropertyMetadata(false));


        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Click += AssociatedObject_Click;
        }

        protected abstract ScrollViewer GetScrollViewer(FrameworkElement associatedScrollableElement);

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            _associatedScrollableElement.UpdateLayout();
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
            _associatedScrollableElement = ((Button)sender).Tag as FrameworkElement;
            if (_associatedScrollableElement == null)
            {
                throw new InvalidOperationException("Button's tag was not specified or it is not a ListView");
            }
            if (_associatedScrollableElement.IsLoaded)
            {
                WireUpAssociatedListView();
            }
            else
            {
                _associatedScrollableElement.Loaded += _associatedScrollableElementLoaded_Loaded;
            }

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }

        private void _associatedScrollableElementLoaded_Loaded(object sender, RoutedEventArgs e)
        {
            WireUpAssociatedListView();
            _associatedScrollableElement.Loaded -= _associatedScrollableElementLoaded_Loaded;
        }

        private void WireUpAssociatedListView()
        {
            _associatedScrollViewer = GetScrollViewer(_associatedScrollableElement);
            if (_associatedScrollViewer != null)
            {
                AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;
                _layoutUpdatedSubscribed = true;
            }
        }

        private void AssociatedObject_LayoutUpdated(object sender, EventArgs e)
        {
            if (_associatedScrollViewer != null)
            {
                if (IsScrollUpButton)
                {
                    AssociatedObject.Visibility = _associatedScrollViewer.ContentVerticalOffset > 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    AssociatedObject.Visibility = _associatedScrollViewer.ContentVerticalOffset < _associatedScrollViewer.ScrollableHeight ?
                    Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        protected override void OnDetaching()
        {
            if (_layoutUpdatedSubscribed)
            {
                AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
            }
            AssociatedObject.Click -= AssociatedObject_Click;
        }
    }
}
