using Clipbox.Behaviors;
using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace Clipbox.Views.ClipboardViewItemTile
{
    [TemplatePart(Name = "PART_SubItemsListView", Type = typeof(ListView))]
    [TemplatePart(Name = "PART_BorderToAnimate", Type = typeof(Border))]
    public class MultiItemPresenter : ItemPresenterBase
    {
        static MultiItemPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiItemPresenter), new FrameworkPropertyMetadata(typeof(MultiItemPresenter)));

            EventManager.RegisterClassHandler(typeof(ListView), ListView.SelectionChangedEvent, new SelectionChangedEventHandler(SubListView_SelectionChanged));
        }

        public ClipboardViewItemInfo ViewItemInfo
        {
            get { return (ClipboardViewItemInfo)GetValue(ViewItemInfoProperty); }
            set { SetValue(ViewItemInfoProperty, value); }
        }

        public static readonly DependencyProperty ViewItemInfoProperty =
            DependencyProperty.Register(nameof(ViewItemInfo), typeof(ClipboardViewItemInfo), typeof(MultiItemPresenter),
                new PropertyMetadata(null));



        public int NumberOfUniformGridColumns
        {
            get { return (int)GetValue(NumberOfUniformGridColumnsProperty); }
            set { SetValue(NumberOfUniformGridColumnsProperty, value); }
        }
        
        public static readonly DependencyProperty NumberOfUniformGridColumnsProperty =
            DependencyProperty.Register(nameof(NumberOfUniformGridColumns), typeof(int), typeof(MultiItemPresenter),
                new PropertyMetadata(0));

        public static readonly RoutedEvent SubItems_SelectionChangedEvent
        = EventManager.RegisterRoutedEvent(
            "SubItems_SelectionChanged",
            RoutingStrategy.Bubble,
            typeof(SelectionChangedEventHandler),
            typeof(MultiItemPresenter));

        public event SelectionChangedEventHandler SubItems_SelectionChanged
        {
            add { AddHandler(SubItems_SelectionChangedEvent, value); }
            remove { RemoveHandler(SubItems_SelectionChangedEvent, value); }
        }
        
        private ListView _subItemsListView;

        private Border _borderToAnimate;

        public MultiItemPresenter()
        {
            ViewItemInfo = new ClipboardViewItemInfo();
            Loaded += MultiItemPresenter_Loaded;
            OnApplyTemplate();
        }

        public override void OnApplyTemplate()
        {
            if (ViewItemWrapper?.IsViewItemLoaded == true)
            {
                _subItemsListView = GetTemplateChild("PART_SubItemsListView") as ListView;
                _borderToAnimate = GetTemplateChild("PART_BorderToAnimate") as Border;
            }
        }
        
        private static void SubListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lv = (ListView)sender;
            lv.RaiseEvent(new SelectionChangedEventArgs(SubItems_SelectionChangedEvent, e.AddedItems, e.RemovedItems));
        }

        private void MultiItemPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            {
                // update these when all binding operations has completed

                if (ViewItemWrapper != null && !ViewItemWrapper.IsViewItemLoaded && ViewItemWrapper.DimensionInfo != null)
                {
                    ViewItemInfo.LoadFromClipboardViewItemWrapper(ViewItemWrapper);
                    UpdateNumberOfUniformGridColumns();
                }
            }));
        }

        protected override void OnViewItemWrapperPropertyChanged(ClipboardViewItemWrapper newValue)
        {
            if (newValue != null && newValue.DimensionInfo != null)
            {
                ViewItemInfo.LoadFromClipboardViewItemWrapper(newValue);
                UpdateNumberOfUniformGridColumns();
            }
        }

        private void UpdateNumberOfUniformGridColumns()
        {
            NumberOfUniformGridColumns = ViewItemInfo.TotalClipboardItemsCount <= 4 ? 2 : 3;
        }

        public void RunNotificationAnimation(AnimationPurpose animationPurpose)
        {
            if (ViewItemWrapper?.IsViewItemLoaded == false)
            {
                return;
            }

            if (_subItemsListView.SelectedItems.Count > 0)
            {
                foreach (var selectedItem in _subItemsListView.SelectedItems)
                {
                    ListViewItem selectedListViewItem = (ListViewItem)_subItemsListView.ItemContainerGenerator.ContainerFromItem(selectedItem);
                    if (!(selectedListViewItem.Template.FindName("Bd", selectedListViewItem) is Border border))
                    {
                        throw new InvalidOperationException("Cannot find an item named Bd in the selected subitem's content template.");
                    }

                    TileAnimationHelper.RunAnimation(border, animationPurpose);
                }
            }
            else
            {
                TileAnimationHelper.RunAnimation(_borderToAnimate, animationPurpose);
            }
        }

        public class ClipboardViewItemInfo : BindableObject
        {
            public ClipboardViewItemInfo()
            {
                _previewItems = new AsyncObservableCollection<ClipboardViewSubItemBase>();
                _previewItems.CollectionChanged += _previewItems_CollectionChanged;
            }

            private void _previewItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                UpdateTotalClipboardItemsCount();
            }

            public void LoadFromClipboardViewItemWrapper(ClipboardViewItemWrapper wrapper)
            {
                if (wrapper == null)
                {
                    throw new ArgumentNullException(nameof(wrapper));
                }

                if (wrapper.DimensionInfo?.Dimensions == null || !wrapper.DimensionInfo.Dimensions.Any())
                {
                    throw new ArgumentException($"{nameof(wrapper.DimensionInfo.Dimensions)} is either null or empty.");
                }

                ViewItemWrapper = wrapper;

                NumberOfItemsWithoutPreview = wrapper.DimensionInfo.NumberOfItemsWithoutPreview;

                PreviewItems.Clear();
                foreach (var item in wrapper.DimensionInfo.Dimensions)
                {
                    PreviewItems.Add(item);
                }

                UpdateTotalClipboardItemsCount();
                UpdatePreviewItemsWithNMoreItem();
            }

            public ClipboardViewItemWrapper _viewItemWrapper;

            public ClipboardViewItemWrapper ViewItemWrapper
            {
                get { return _viewItemWrapper; }
                set { SetField(ref _viewItemWrapper, value); }
            }

            private AsyncObservableCollection<ClipboardViewSubItemBase> _previewItems;

            public AsyncObservableCollection<ClipboardViewSubItemBase> PreviewItems
            {
                get { return _previewItems; }
                set { SetField(ref _previewItems, value); }
            }

            public int _totalClipboardItemsCount;

            public int TotalClipboardItemsCount
            {
                get { return _totalClipboardItemsCount; }
                set { SetField(ref _totalClipboardItemsCount, value); }
            }

            public int _numberOfItemsWithoutPreview;

            public int NumberOfItemsWithoutPreview
            {
                get { return _numberOfItemsWithoutPreview; }
                private set { SetField(ref _numberOfItemsWithoutPreview, value); }
            }

            private void UpdatePreviewItemsWithNMoreItem()
            {
                var existingItem = _previewItems
                        .FirstOrDefault(item => item is ClipboardViewSubItemNMore);

                if (existingItem != null)
                {
                    if (NumberOfItemsWithoutPreview == 0)
                    {
                        _previewItems.Remove(existingItem);
                        return;
                    }

                    ((ClipboardViewSubItemNMore)existingItem).NumberOfItems = NumberOfItemsWithoutPreview;

                }
                else if (NumberOfItemsWithoutPreview > 0)
                {
                    _previewItems.Add(new ClipboardViewSubItemNMore { NumberOfItems = NumberOfItemsWithoutPreview });
                }
            }

            private void UpdateTotalClipboardItemsCount()
            {
                TotalClipboardItemsCount = PreviewItems
                    .Count(item => item is ClipboardViewSubItemDimension)
                    + NumberOfItemsWithoutPreview;
            }

        }
    }
}
