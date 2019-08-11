using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for MainViewClipboardContents.xaml
    /// </summary>
    public partial class MainViewClipboardContents : ClipboardViewControlBase, IDisposable
    {
        private const int LAYOUT_TIMER_TIMEOUT_MS = 100;
        private const int SCROLLBAR_TIMER_TIMEOUT_MS = 300;
        private SlidingTimer _layoutSlidingTimer;
        private SlidingTimer _scrollbarValueChangedSlidingTimer;


        private ScrollBar _scrollBar;

        private MainViewClipboardContentsViewModel _viewModel;
        
        private int _lastListViewItemCount = 0;
        private Dictionary<string, GroupItem> _groupNameGroupItemMapping = new Dictionary<string, GroupItem>();
        
        public MainViewClipboardContents(MainViewClipboardContentsViewModel vm, 
            UIExtentionContainer container, 
            ISettingsService settingsService)
            : base (container, settingsService)
        {
            InitializeComponent();
            _viewModel = vm;
            DataContext = _viewModel;

            _layoutSlidingTimer = new SlidingTimer();
            _layoutSlidingTimer.IntervalMs = LAYOUT_TIMER_TIMEOUT_MS;
            _layoutSlidingTimer.Elapsed += _layoutSlidingTimer_Elapsed;

            _scrollbarValueChangedSlidingTimer = new SlidingTimer();
            _scrollbarValueChangedSlidingTimer.IntervalMs = SCROLLBAR_TIMER_TIMEOUT_MS;
            _scrollbarValueChangedSlidingTimer.Elapsed += _scrollbarValueChangedSlidingTimer_Elapsed; ;


            IsVisibleChanged += MainViewClipboardContents_IsVisibleChanged;

            MainListView.LayoutUpdated += MainListView_LayoutUpdated;
        }

        private void _scrollbarValueChangedSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            LoadUnloadFullViewOfVisibleItems();
        }

        private void _layoutSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            LoadItemsAndGroupsCachedInfo(MainListView);
        }

        private void MainListView_LayoutUpdated(object sender, EventArgs e)
        {
            _layoutSlidingTimer.Slide();
        }

        private void MainViewClipboardContents_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                _viewModel.LoadAllItems(this.MonitorDimensions);
            }
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

            if (MainListView == null)
            {
                return false; // is the listview loaded at this point?
            }

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
                Point groupItemRelativeLocation = groupItem.TranslatePoint(new Point(0, 0), MainListView);

                if (thumbYPos + thumb.ActualHeight >= groupItemRelativeLocation.Y &&
                    thumbYPos <= groupItemRelativeLocation.Y + groupItem.ActualHeight)
                {
                    _viewModel.CurrentVisibleItemsDateString = groupName;
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
                groupItem.TransformToAncestor(MainListView).TransformBounds(new Rect(0.0, 0.0, groupItem.ActualWidth, groupItem.ActualHeight));
                var rect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
                if (rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight))
                {
                    _visibleGroupNames.Add(groupName);
                }
            }
        }

        private void LoadItemsAndGroupsCachedInfo(ListView listView)
        {
            if (_lastListViewItemCount == listView.Items.Count)
            {
                return;
            }

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);

            if (view.Groups == null)
            {
                return;
            }
            
            _groupNameGroupItemMapping.Clear();

            var groups = view.Groups.Cast<CollectionViewGroup>();
            
            if (listView.Items.Count == 0)
            {
                return;
            }

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

                ListViewItem firstLvi = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(group.Items.First());
                
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

            _viewModel.FirstItemDateString = _groupNameGroupItemMapping.First().Key;
            _viewModel.LastItemDateString = _groupNameGroupItemMapping.Last().Key;

            _lastListViewItemCount = listView.Items.Count;
            
            TryAdjustScrollThumbLabel();
            LoadUnloadFullViewOfVisibleItems();
        }

        private void LoadUnloadFullViewOfVisibleItems()
        {
            if (MainListView == null)
            {
                return; // is the listview loaded at this point?
            }

            // filter out the groups that are not currently in view
            var visibleGroupItemMapping = _groupNameGroupItemMapping
                .Where(kvp => _visibleGroupNames.Contains(kvp.Key))
                .ToDictionary(k => k.Key, v => v.Value);

            foreach (ClipboardViewItemWrapper lviVm in MainListView.Items)
            {
                // if the item is part of a visible group...
                // The item's group name is the wrapper's DateCreatedFriendly.
                if (visibleGroupItemMapping.ContainsKey(lviVm.DateCreatedFriendly))
                {
                    // load the actual item
                    ListViewItem lvi = (ListViewItem)MainListView.ItemContainerGenerator.ContainerFromItem(lviVm);
                    
                    // check if the item is within the bounds of the clipboard view
                    Rect bounds =
                        lvi.TransformToAncestor(MainListView).TransformBounds(new Rect(0.0, 0.0, lvi.ActualWidth, lvi.ActualHeight));
                    var rect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
                    if ((rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight))
                        && !lviVm.IsViewItemLoaded)
                    {
                        // load the full view item
                        _viewModel.ScheduleLoadingOfViewItem(lviVm.Id, this.MonitorDimensions);
                    }
                }
                else
                {
                    _viewModel.CancelLoadingOfViewItem(lviVm.Id);

                    if (lviVm.IsViewItemLoaded)
                    {
                        // unload the item to save up memory
                        _viewModel.UnloadViewItem(lviVm.Id);
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
            _scrollBar = sender as ScrollBar;
        }
        
        /// <summary>
        /// This event is wired to the GroupItem's control template main container. We show the listview only when the grouping is ready.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            MainListView.Visibility = Visibility.Visible;
        }

        public void Dispose()
        {
            _layoutSlidingTimer.Dispose();
            _scrollbarValueChangedSlidingTimer.Dispose();
        }

        ~MainViewClipboardContents()
        {
            Debug.WriteLine("MainViewClipboardContents unloaded");
        }
    }
}
