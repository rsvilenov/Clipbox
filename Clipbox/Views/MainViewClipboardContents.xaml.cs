using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.Interfaces;
using Clipbox.ViewModels;
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
    public partial class MainViewClipboardContents : ClipboardViewControl
    {
        private const int LAYOUT_TIMER_TIMEOUT_MS = 100;
        private SlidingTimer _layoutSlidingTimer;

        private MainClipboardViewViewModel _viewModel;
        
        private int _lastListViewItemCount = 0;
        private Dictionary<string, GroupItem> _groupNameGroupItemMapping = new Dictionary<string, GroupItem>();
        
        public MainViewClipboardContents(MainClipboardViewViewModel vm, UIExtentionContainer container, ISettingsService settingsService)
            : base (container, settingsService)
        {
            InitializeComponent();
            _viewModel = vm;
            DataContext = _viewModel;

            _layoutSlidingTimer = new SlidingTimer();
            _layoutSlidingTimer.IntervalMs = LAYOUT_TIMER_TIMEOUT_MS;
            _layoutSlidingTimer.Elapsed += _layoutSlidingTimer_Elapsed;

            IsVisibleChanged += MainViewClipboardContents_IsVisibleChanged;

            MainListView.LayoutUpdated += MainListView_LayoutUpdated;
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
            _viewModel.LoadItems();
        }

        private void PART_VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScrollBar scrollBar = sender as ScrollBar;
            AdjustScrollThumbLabel(scrollBar);
        }

        private void AdjustScrollThumbLabel(UIElement scrollElement)
        {
            // find the listview control
            ListView listView = Traversing.FindParent<ListView>(scrollElement);
            if (listView == null)
            {
                Debug.WriteLine("listView (still) not found.");
                return;
            }

            // find the thumb and its location
            Thumb thumb = Traversing.GetVisualChildOfType<Thumb>(scrollElement);
            if (thumb == null)
            {
                Debug.WriteLine("thumb (still) not found.");
                return;
            }

            Point relativeThumbPoint = thumb.TranslatePoint(new Point(0, 0), scrollElement);
            double thumbYPos = relativeThumbPoint.Y;

            foreach (string groupName in _groupNameGroupItemMapping.Keys)
            {
                GroupItem groupItem = _groupNameGroupItemMapping[groupName];
                Point groupItemRelativeLocation = groupItem.TranslatePoint(new Point(0, 0), listView);

                if (thumbYPos + thumb.ActualHeight >= groupItemRelativeLocation.Y &&
                    thumbYPos <= groupItemRelativeLocation.Y + groupItem.ActualHeight)
                {
                    _viewModel.CurrentVisibleItemsDateString = groupName;
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

                _groupNameGroupItemMapping.Add(groupName, groupItem);
            }

            _viewModel.FirstItemDateString = _groupNameGroupItemMapping.First().Key;
            _viewModel.LastItemDateString = _groupNameGroupItemMapping.Last().Key;

            _lastListViewItemCount = listView.Items.Count;
        }

        private void PART_ScrollContentPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollContentPresenter scrollContentPresenter = sender as ScrollContentPresenter;
            if (scrollContentPresenter.ScrollOwner != null)
            {
                AdjustScrollThumbLabel(scrollContentPresenter.ScrollOwner);
            }
        }
    }
}
