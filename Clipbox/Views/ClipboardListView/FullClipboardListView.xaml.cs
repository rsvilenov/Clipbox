using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.EventArgsClasses;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Clipbox.Views.ClipboardListView
{
    /// <summary>
    /// Interaction logic for MainClipboardListView.xaml
    /// </summary>
    public partial class FullClipboardListView : ClipboardListViewBase, IDisposable
    {
        private const int LAYOUT_TIMER_TIMEOUT_MS = 100;
        private const int SCROLLBAR_TIMER_TIMEOUT_MS = 300;
        
        public event EventHandler<ClipboardItemManipulationEventArgs> ViewItemLoadingRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ViewItemUnloadingRequested;

        public event EventHandler<ClipboardItemManipulationEventArgs> ViewItemLoadingCancelRequested;

        public event EventHandler<EventArgs> GroupingComplete;

        public event EventHandler<KeyEventArgs> ClipboardItemsPanel_KeyUp;

        public event EventHandler<EventArgs> ClipboardItemContentControl_MouseDoubleClickDetectionStarted;

        public event EventHandler<EventArgs> ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected;

        public event EventHandler<EventArgs> ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected;

        public int DoubleClickDetectionWaitIntervalMs;
        
        private Timer _postSelectionUtilsTimer;

        private SlidingTimer _scrollbarValueChangedSlidingTimer;

        private SlidingTimer _layoutSlidingTimer;

        private ScrollBar _scrollBar;

        private int _lastListViewItemCount = 0;
        private Dictionary<string, GroupItem> _groupNameGroupItemMapping = new Dictionary<string, GroupItem>();

        
        private ScrollBarDataContextClass ScrollBarDataContext
        {
            get { return (ScrollBarDataContextClass)GetValue(ScrollBarDataContextProperty); }
            set { SetValue(ScrollBarDataContextProperty, value); }
        }

        private static readonly DependencyProperty ScrollBarDataContextProperty =
            DependencyProperty.Register(nameof(ScrollBarDataContext), typeof(ScrollBarDataContextClass), typeof(FullClipboardListView), new PropertyMetadata(null));

        /// <summary>
        /// This view is the only one that allows for selection of incompatible items,
        /// so this property seem to belong here.
        /// </summary>
        public bool AreSelectedItemsCombinable
        {
            get { return (bool)GetValue(AreSelectedItemsCombinableProperty); }
            set { SetValue(AreSelectedItemsCombinableProperty, value); }
        }

        public static readonly DependencyProperty AreSelectedItemsCombinableProperty =
            DependencyProperty.Register(nameof(AreSelectedItemsCombinable), typeof(bool), typeof(FullClipboardListView), new PropertyMetadata(true));

        
        public bool IsCloudSyncAvailable
        {
            get { return (bool)GetValue(IsCloudSyncAvailableProperty); }
            set { SetValue(IsCloudSyncAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsCloudSyncAvailableProperty =
            DependencyProperty.Register(nameof(IsCloudSyncAvailable), typeof(bool), typeof(FullClipboardListView), new PropertyMetadata(false));

        
        public FullClipboardListView()
        {
            InitializeComponent();

            ScrollBarDataContext = new ScrollBarDataContextClass();

            _layoutSlidingTimer = new SlidingTimer
            {
                IntervalMs = LAYOUT_TIMER_TIMEOUT_MS
            };
            _layoutSlidingTimer.Elapsed += _layoutSlidingTimer_Elapsed;

            _scrollbarValueChangedSlidingTimer = new SlidingTimer
            {
                IntervalMs = SCROLLBAR_TIMER_TIMEOUT_MS
            };
            _scrollbarValueChangedSlidingTimer.Elapsed += _scrollbarValueChangedSlidingTimer_Elapsed;

            DoubleClickDetectionWaitIntervalMs = User32.GetDoubleClickTime();

            _postSelectionUtilsTimer = new Timer
            {
                AutoReset = false,
                Interval = DoubleClickDetectionWaitIntervalMs
            };

            _postSelectionUtilsTimer.Elapsed += _postSelectionUtilsTimer_Elapsed;

            LayoutUpdated += (sender, args) => _layoutSlidingTimer.Slide();
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            // when we perform search or add/remove items, we should adjust the group cache
            InvalidateGroupCache();
            base.OnItemsChanged(e);
        }

        private void InvalidateGroupCache()
        {
            // causes LoadItemsAndGroupsCachedInfo() to reload the cache on the next LayoutUpdated event.
            _lastListViewItemCount = 0;
            _groupNameGroupItemMapping.Clear();
        }

        private void _scrollbarValueChangedSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            LoadUnloadFullViewOfVisibleItems();
        }

        private void _layoutSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            LoadItemsAndGroupsCachedInfo();
        }

        private void _postSelectionUtilsTimer_Elapsed(object sender, EventArgs e)
        {
            ClipboardItemContentControl_MouseDoubleClickDetection_SingleClickDetected?.Invoke(this, e);
        }

        protected override void OnSelectedItemsCombinabilityChanged(SelectedItemsCombinabilityArgs args)
        {
            AreSelectedItemsCombinable = args.AreItemsCombinable;
        }

        /// <summary>
        /// This event is wired to the GroupItem's control template main container.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            GroupingComplete?.Invoke(this, new EventArgs());
            ((StackPanel)sender).Loaded -= StackPanel_Loaded;
        }
        
        private void WrapPanel_KeyUp(object sender, KeyEventArgs e)
        {
            ClipboardItemsPanel_KeyUp?.Invoke(this, e);
        }

        private void ContentControl_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                ClipboardItemContentControl_MouseDoubleClickDetectionStarted?.Invoke(this, e);
                _postSelectionUtilsTimer.Start();
            }
            else
            {
                _postSelectionUtilsTimer.Stop();

                // if the item has been previously selected, the first click will unselect it,
                // so that we need to re-select it on the second click in order for it to be in
                // the list with selected items.
                var contentControl = (ContentControl)sender;
                var itemWrapper = (ClipboardViewItemWrapper)contentControl.DataContext;
                if (!itemWrapper.IsSelected)
                {
                    itemWrapper.IsSelected = true;
                    this.SelectedItems.Add(itemWrapper);
                }

                // cancel the second click in a doubleclick so that it won't deselect the item
                e.Handled = true;

                ClipboardItemContentControl_MouseDoubleClickDetection_DoubleClickDetected?.Invoke(this, e);
            }
        }

        private void ContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var contentControl = (ContentControl)sender;
            contentControl.PreviewMouseLeftButtonDown -= ContentControl_PreviewMouseLeftButtonDown;
            contentControl.Unloaded -= ContentControl_Unloaded;
        }

        private void PART_VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TryAdjustScrollThumbLabel();
            _scrollbarValueChangedSlidingTimer.Slide();
        }

        private bool TryAdjustScrollThumbLabel()
        {
            if (_scrollBar == null)
            {
                return false;
            }

            //// find the listview control
            //ListView listView = Traversing.FindParent<ListView>(_scrollBar);
            //if (listView == null)
            //{
            //    Debug.WriteLine("listView (still) not found.");
            //    return false;
            //}

            UpdateVisibleGroupsCache();

            // find the thumb and its location
            Thumb thumb = Traversing.GetVisualChildOfType<Thumb>(_scrollBar);
            if (thumb == null)
            {
                Debug.WriteLine("thumb (still) not found.");
                return false;
            }

            Point relativeThumbPoint = thumb.TranslatePoint(new Point(0, 0), _scrollBar);
            double thumbYPos = relativeThumbPoint.Y;

            foreach (string groupName in _visibleGroupNames)
            {
                if (groupName == string.Empty)
                {
                    break; // not all groups are loaded
                }

                GroupItem groupItem = _groupNameGroupItemMapping[groupName];
                Point groupItemRelativeLocation = groupItem.TranslatePoint(new Point(0, 0), this);

                if (thumbYPos + thumb.ActualHeight >= groupItemRelativeLocation.Y &&
                    thumbYPos <= groupItemRelativeLocation.Y + groupItem.ActualHeight)
                {
                    ScrollBarDataContext.CurrentVisibleItemsDateString = groupName;
                }
            }

            return true;
        }

        private List<string> _visibleGroupNames = new List<string>();
        private void UpdateVisibleGroupsCache()
        {
            _visibleGroupNames = new List<string>();

            foreach (string groupName in _groupNameGroupItemMapping.Keys)
            {
                GroupItem groupItem = _groupNameGroupItemMapping[groupName];

                Rect bounds =
                groupItem.TransformToAncestor(this).TransformBounds(new Rect(0.0, 0.0, groupItem.ActualWidth, groupItem.ActualHeight));
                var rect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
                
                if (/* if the group is within the listview's bounds */
                rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight)
                 /* or if the group is larger than the listview's height 
                  * and its top and bottom are not within the visible area */
                 || (bounds.Top < rect.Top && bounds.Bottom > rect.Bottom))
                {
                    _visibleGroupNames.Add(groupName);
                }
            }
        }

        private void LoadItemsAndGroupsCachedInfo()
        {
            if (_lastListViewItemCount == this.Items.Count)
            {
                return;
            }

            Debug.WriteLine("Loading group cached info.");

            _groupNameGroupItemMapping.Clear();


            if (this.Items.Count == 0)
            {
                return;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(this.ItemsSource);

            if (view.Groups == null)
            {
                return;
            }

            var groups = view.Groups.Cast<CollectionViewGroup>();


            foreach (var group in groups)
            {
                if (group.Name is string == false)
                {
                    throw new InvalidCastException("Group name should be string.");
                }

                string groupName = (string)group.Name;
                if (_groupNameGroupItemMapping.ContainsKey(groupName))
                {
                    throw new ArgumentException("Group names should be unique");
                }

                ListViewItem firstLvi = (ListViewItem)this.ItemContainerGenerator.ContainerFromItem(group.Items.First());

                GroupItem groupItem = Traversing.FindParent<GroupItem>(firstLvi);

                if (groupItem == null)
                {
                    // the item has not been loaded yet.
                    // TODO: maybe improve this code some day by
                    // finding a way to wait for the item to laod
                    // and not just skip it...
                    continue;
                }

                _groupNameGroupItemMapping.Add(groupName, groupItem);
            }

            ScrollBarDataContext.FirstItemDateString = _groupNameGroupItemMapping.First().Key;
            ScrollBarDataContext.LastItemDateString = _groupNameGroupItemMapping.Last().Key;

            _lastListViewItemCount = this.Items.Count;

            TryAdjustScrollThumbLabel();
            LoadUnloadFullViewOfVisibleItems();
        }

        private void LoadUnloadFullViewOfVisibleItems()
        {
            // filter out the groups that are not currently in view
            var visibleGroupItemMapping = _groupNameGroupItemMapping
                .Where(kvp => _visibleGroupNames.Contains(kvp.Key))
                .ToDictionary(k => k.Key, v => v.Value);

            foreach (ClipboardViewItemWrapper lviVm in this.Items)
            {
                // if the item is part of a visible group...
                // The item's group name is the wrapper's DateCreatedFriendly.
                if (visibleGroupItemMapping.ContainsKey(lviVm.DateCreatedFriendly))
                {
                    // load the actual item
                    ListViewItem lvi = (ListViewItem)this.ItemContainerGenerator.ContainerFromItem(lviVm);

                    // check if the item is within the bounds of the clipboard view
                    Rect bounds =
                        lvi.TransformToAncestor(this).TransformBounds(new Rect(0.0, 0.0, lvi.ActualWidth, lvi.ActualHeight));
                    var rect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
                    if ((rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight))
                        && !lviVm.IsViewItemLoaded)
                    {
                        // load the full view item
                        ViewItemLoadingRequested?.Invoke(this, new ClipboardItemManipulationEventArgs(lviVm.Id));
                    }
                }
                else
                {
                    ViewItemLoadingCancelRequested?.Invoke(this, new ClipboardItemManipulationEventArgs(lviVm.Id));

                    if (lviVm.IsViewItemLoaded
                        && !lviVm.IsSelected  /* do not unload the selected items */
                        && !SelectedMultiItemsIds.ContainsKey(lviVm.Id) /* do not unload the multi items with selected subitems */)
                    {
                        // unload the item to save up memory
                        ViewItemUnloadingRequested?.Invoke(this, new ClipboardItemManipulationEventArgs(lviVm.Id));
                    }
                }
            }
        }

        private void PART_ScrollContentPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TryAdjustScrollThumbLabel();
            LoadUnloadFullViewOfVisibleItems();
        }

        private void PART_VerticalScrollBar_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollBar = (ScrollBar)sender;
        }

        public void Dispose()
        {
            _postSelectionUtilsTimer.Dispose();
            _scrollbarValueChangedSlidingTimer.Dispose();
            _layoutSlidingTimer.Dispose();
        }

        private class ScrollBarDataContextClass : BindableObject
        {
            private string _currentVisibleItemsDateString;

            public string CurrentVisibleItemsDateString
            {
                get { return _currentVisibleItemsDateString; }
                set { SetField(ref _currentVisibleItemsDateString, value); }
            }

            private string _firstItemDateString;

            public string FirstItemDateString
            {
                get { return _firstItemDateString; }
                set { SetField(ref _firstItemDateString, value); }
            }

            private string _lastItemDateString;

            public string LastItemDateString
            {
                get { return _lastItemDateString; }
                set { SetField(ref _lastItemDateString, value); }
            }
        }

    }
}
