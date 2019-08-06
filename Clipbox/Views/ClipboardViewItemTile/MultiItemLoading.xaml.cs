using Clipbox.Infrastructure.UI;
using Clipbox.ViewModels.BindableObjects;
using Clipbox.ViewModels.ClipboardViewItemTile;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.ClipboardViewItemTile
{
    /// <summary>
    /// Interaction logic for MultiItemLoading.xaml
    /// </summary>
    public partial class MultiItemLoading : UserControl
    {
        private MultiItemLoadingViewModel _viewModel;
        private bool viewSubItemDimensions_CollectionChanged_subscribed;

        public MultiItemLoading()
        {
            _viewModel = new MultiItemLoadingViewModel();
            
            InitializeComponent();

            mainGrid.DataContext = _viewModel;

            Loaded += MultiItemLoading_Loaded;
        }

        private void MultiItemLoading_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, new Action(() =>
            {
                // call this once when the bindings are complete
                _viewModel.UpdatePreviewItemsWithNMoreItem();
            }));
        }

        public int NumberOfItemsWithoutPreview
        {
            get { return (int)GetValue(NumberOfItemsWithoutPreviewProperty); }
            set { SetValue(NumberOfItemsWithoutPreviewProperty, value); }
        }
        
        public static readonly DependencyProperty NumberOfItemsWithoutPreviewProperty =
            DependencyProperty.Register(nameof(NumberOfItemsWithoutPreview), typeof(int), typeof(MultiItemLoading), new PropertyMetadata(0, NumberOfItemsWithoutPreview_PropertyChanged));

        private static void NumberOfItemsWithoutPreview_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MultiItemLoading)d;
            if (!self.viewSubItemDimensions_CollectionChanged_subscribed)
                self.subscribe();

            self._viewModel.NumberOfItemsWithoutPreview = (int)e.NewValue;
        }

        private void subscribe()
        {
            if (ViewSubItemDimensions != null)
            {
                ViewSubItemDimensions.CollectionChanged += ViewSubItemDimensions_CollectionChanged;
                viewSubItemDimensions_CollectionChanged_subscribed = true;
            }
        }

        public AsyncObservableCollection<ClipboardViewSubItemDimension> ViewSubItemDimensions
        {
            get { return (AsyncObservableCollection<ClipboardViewSubItemDimension>)GetValue(ViewSubItemDimensionsProperty); }
            set { SetValue(ViewSubItemDimensionsProperty, value); }
        }

        public static readonly DependencyProperty ViewSubItemDimensionsProperty =
            DependencyProperty.Register(nameof(ViewSubItemDimensions), typeof(AsyncObservableCollection<ClipboardViewSubItemDimension>), typeof(MultiItemLoading), 
                new PropertyMetadata(null, ViewSubItemDimensions_PropertyChanged));

        private static void ViewSubItemDimensions_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MultiItemLoading)d;
            self.SyncVmCollection();
        }
        
        private void ViewSubItemDimensions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SyncVmCollection();
        }

        private void SyncVmCollection()
        {
            if (ViewSubItemDimensions == null || !ViewSubItemDimensions.Any())
            {
                return;
            }

            _viewModel.PreviewItems.Clear();
            foreach (var item in ViewSubItemDimensions)
            {
                _viewModel.PreviewItems.Add(item);
            }
        }
    }
}
