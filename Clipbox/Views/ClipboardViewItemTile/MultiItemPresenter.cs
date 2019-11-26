using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels.BindableObjects;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public class MultiItemPresenter : ItemPresenterBase
    {
        static MultiItemPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultiItemPresenter), new FrameworkPropertyMetadata(typeof(MultiItemPresenter)));
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


        public MultiItemPresenter()
        {
            ViewItemInfo = new ClipboardViewItemInfo();
            Loaded += MultiItemPresenter_Loaded;
        }

        private void MultiItemPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            {
                // update these when all binding operations has completed

                if (ViewItemWrapper != null && ViewItemWrapper.DimensionInfo != null)
                {
                    ViewItemInfo.LoadFromClipboardViewItemWrapper(ViewItemWrapper);
                    UpdateNumberOfUniformGridColumns();
                }
            }));
        }

        public override void OnApplyTemplate()
        {
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
