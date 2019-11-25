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

        public MultiItemLoadingSubItemsInfo SubItemsInfo
        {
            get { return (MultiItemLoadingSubItemsInfo)GetValue(SubItemsInfoProperty); }
            set { SetValue(SubItemsInfoProperty, value); }
        }

        public static readonly DependencyProperty SubItemsInfoProperty =
            DependencyProperty.Register(nameof(SubItemsInfo), typeof(MultiItemLoadingSubItemsInfo), typeof(MultiItemPresenter),
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
            SubItemsInfo = new MultiItemLoadingSubItemsInfo();
            Loaded += MultiItemPresenter_Loaded;
        }

        private void MultiItemPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            {
                // update these when all binding operations has completed

                if (ViewItemWrapper != null && ViewItemWrapper.DimensionInfo != null)
                {
                    SubItemsInfo.LoadFromViewDimensionInfo(ViewItemWrapper.DimensionInfo);
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
                SubItemsInfo.LoadFromViewDimensionInfo(newValue.DimensionInfo);
                UpdateNumberOfUniformGridColumns();
            }
        }

        private void UpdateNumberOfUniformGridColumns()
        {
            NumberOfUniformGridColumns = SubItemsInfo.TotalClipboardItemsCount <= 4 ? 2 : 3;
        }

        public class MultiItemLoadingSubItemsInfo : BindableObject
        {
            public MultiItemLoadingSubItemsInfo()
            {
                _previewItems = new AsyncObservableCollection<ClipboardViewSubItemBase>();
                _previewItems.CollectionChanged += _previewItems_CollectionChanged;
            }

            private void _previewItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                UpdateTotalClipboardItemsCount();
            }

            public void LoadFromViewDimensionInfo(ClipboardViewItemDimensionInfo dimensionInfo)
            {
                if (dimensionInfo == null)
                {
                    throw new ArgumentNullException(nameof(dimensionInfo));
                }

                if (dimensionInfo.Dimensions == null || !dimensionInfo.Dimensions.Any())
                {
                    throw new ArgumentException($"{nameof(dimensionInfo.Dimensions)} is either null or empty.");
                }

                NumberOfItemsWithoutPreview = dimensionInfo.NumberOfItemsWithoutPreview;

                PreviewItems.Clear();
                foreach (var item in dimensionInfo.Dimensions)
                {
                    PreviewItems.Add(item);
                }

                UpdateTotalClipboardItemsCount();
                UpdatePreviewItemsWithNMoreItem();
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
