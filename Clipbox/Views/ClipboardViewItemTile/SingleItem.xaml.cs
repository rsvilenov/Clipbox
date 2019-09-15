using Clipbox.ViewModels.BindableObjects;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.ClipboardViewItemTile
{
    /// <summary>
    /// Interaction logic for SingleItem.xaml
    /// </summary>
    public partial class SingleItem : UserControl
    {
        public SingleItem()
        {
            InitializeComponent();

            mainGrid.DataContext = this;
        }



        public ClipboardViewItemSingle ClipboardViewItem
        {
            get { return (ClipboardViewItemSingle)GetValue(ClipboardViewItemProperty); }
            set { SetValue(ClipboardViewItemProperty, value); }
        }

        public static readonly DependencyProperty ClipboardViewItemProperty =
            DependencyProperty.Register(nameof(ClipboardViewItem), typeof(ClipboardViewItemSingle), typeof(SingleItem), new PropertyMetadata(null, ClipboardViewItem_ValueChanged));

        private static void ClipboardViewItem_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }

        public bool IsDimmed
        {
            get { return (bool)GetValue(IsDimmedProperty); }
            set { SetValue(IsDimmedProperty, value); }
        }

        public static readonly DependencyProperty IsDimmedProperty =
            DependencyProperty.Register(nameof(IsDimmed), typeof(bool), typeof(SingleItem), new PropertyMetadata(false));




        public double ThumbnailAreaMaxHeight
        {
            get { return (double)GetValue(ThumbnailAreaMaxHeightProperty); }
            set { SetValue(ThumbnailAreaMaxHeightProperty, value); }
        }
        
        public static readonly DependencyProperty ThumbnailAreaMaxHeightProperty =
            DependencyProperty.Register(nameof(ThumbnailAreaMaxHeight), typeof(double), typeof(SingleItem), new PropertyMetadata(0d));



        public double ThumbnailAreaMinHeight
        {
            get { return (double)GetValue(ThumbnailAreaMinHeightProperty); }
            set { SetValue(ThumbnailAreaMinHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailAreaMinHeightProperty =
            DependencyProperty.Register(nameof(ThumbnailAreaMinHeight), typeof(double), typeof(SingleItem), new PropertyMetadata(0d));
        
    }
}
