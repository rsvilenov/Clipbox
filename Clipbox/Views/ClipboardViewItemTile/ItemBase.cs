using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public abstract class ItemBase : UserControl
    {
        public Color PreviewContainerBackgroundColor
        {
            get { return (Color)GetValue(PreviewContainerBackgroundColorProperty); }
            set { SetValue(PreviewContainerBackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty PreviewContainerBackgroundColorProperty =
            DependencyProperty.Register(nameof(PreviewContainerBackgroundColor), typeof(Color), typeof(ItemBase),
                /* the color should not be null, because we need the container to be hit-testable (for mouse events to fire) */
                new PropertyMetadata(Colors.Transparent));

        
        public bool IsFadeInEnabled
        {
            get { return (bool)GetValue(IsFadeInEnabledProperty); }
            set { SetValue(IsFadeInEnabledProperty, value); }
        }
        
        public static readonly DependencyProperty IsFadeInEnabledProperty =
            DependencyProperty.Register(nameof(IsFadeInEnabled), typeof(bool), typeof(ItemBase), new PropertyMetadata(false));

    }
}
