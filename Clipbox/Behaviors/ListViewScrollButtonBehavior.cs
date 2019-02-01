using Clipbox.Infrastructure.UI.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Clipbox.Behaviors
{
    public class ListViewScrollButtonBehavior : Behavior<Button>
    {
        private ListView _associatedListView;
        private ScrollViewer _associatedListViewScrollViewer;
        bool _associatedListViewLoaded;
        
        public bool ScrollUp
        {
            get { return (bool)GetValue(ScrollUpProperty); }
            set { SetValue(ScrollUpProperty, value); }
        }
        
        public static readonly DependencyProperty ScrollUpProperty =
            DependencyProperty.Register("ScrollUp", typeof(bool), typeof(ListViewScrollButtonBehavior), new PropertyMetadata(false));
        

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.Click += AssociatedObject_Click;
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            _associatedListView.UpdateLayout();
            if (_associatedListViewScrollViewer != null)
            {
                if (ScrollUp)
                {
                    _associatedListViewScrollViewer.PageUp();
                }
                else
                {
                    _associatedListViewScrollViewer.PageDown();
                }
            }
        }

        private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _associatedListView = ((Button)sender).Tag as ListView;
            if (_associatedListView == null)
            {
                throw new InvalidOperationException("Button's tag was not specified or it is not a ListView");
            }
            if (_associatedListView.IsLoaded)
            {
                WireUpAssociatedListView();
            }
            else
            {
                _associatedListView.Loaded += _associatedListView_Loaded;
            }

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }

        private void _associatedListView_Loaded(object sender, RoutedEventArgs e)
        {
            WireUpAssociatedListView();
            _associatedListView.Loaded -= _associatedListView_Loaded;
        }

        private void WireUpAssociatedListView()
        {
            _associatedListViewScrollViewer = Traversing.GetVisualChildOfType<ScrollViewer>(_associatedListView);
            AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;
            _associatedListViewLoaded = true;
        }

        private void AssociatedObject_LayoutUpdated(object sender, EventArgs e)
        {
            if (_associatedListViewScrollViewer != null)
            {
                if (ScrollUp)
                {
                    AssociatedObject.Visibility = _associatedListViewScrollViewer.ContentVerticalOffset > 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    AssociatedObject.Visibility = _associatedListViewScrollViewer.ContentVerticalOffset < _associatedListViewScrollViewer.ScrollableHeight ?
                    Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        protected override void OnDetaching()
        {
            if (_associatedListViewLoaded)
            {
                AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
            }
            AssociatedObject.Click -= AssociatedObject_Click;
        }
    }
}
