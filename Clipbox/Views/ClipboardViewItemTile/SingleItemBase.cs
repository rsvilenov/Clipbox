using System.Windows;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public class SingleItemBase : ItemBase
    {
        public double ThumbnailAreaMaxHeight
        {
            get { return (double)GetValue(ThumbnailAreaMaxHeightProperty); }
            set { SetValue(ThumbnailAreaMaxHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailAreaMaxHeightProperty =
            DependencyProperty.Register(nameof(ThumbnailAreaMaxHeight), typeof(double), typeof(SingleItemBase), new PropertyMetadata(0d));



        public double ThumbnailAreaMinHeight
        {
            get { return (double)GetValue(ThumbnailAreaMinHeightProperty); }
            set { SetValue(ThumbnailAreaMinHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailAreaMinHeightProperty =
            DependencyProperty.Register(nameof(ThumbnailAreaMinHeight), typeof(double), typeof(SingleItemBase), new PropertyMetadata(0d));

    }
}
