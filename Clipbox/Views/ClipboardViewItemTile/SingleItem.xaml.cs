using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using System.Windows;

namespace Clipbox.Views.ClipboardViewItemTile
{
    /// <summary>
    /// Interaction logic for SingleItem.xaml
    /// </summary>
    public partial class SingleItem : SingleItemBase, IAnimatableItem
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
            DependencyProperty.Register(nameof(ClipboardViewItem), typeof(ClipboardViewItemSingle), typeof(SingleItem), new PropertyMetadata(null));
        
        public void RunNotificationAnimation(AnimationPurpose animationPurpose)
        {
            TileAnimationHelper.RunAnimation(borderToAnimate, animationPurpose);
        }

        public bool IsDimmed
        {
            get { return (bool)GetValue(IsDimmedProperty); }
            set { SetValue(IsDimmedProperty, value); }
        }

        public static readonly DependencyProperty IsDimmedProperty =
            DependencyProperty.Register(nameof(IsDimmed), typeof(bool), typeof(SingleItem), new PropertyMetadata(false));
        
    }
}
