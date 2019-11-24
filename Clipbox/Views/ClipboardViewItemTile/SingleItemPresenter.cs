using Clipbox.ViewModels.BindableObjects;
using Clipbox.Views.Enums.ClipboardViewItemTile;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.ClipboardViewItemTile
{
    [TemplatePart(Name = "PART_BorderToAnimate", Type = typeof(Border))]
    public class SingleItemPresenter : ItemPresenterBase, IAnimatableItem
    {
        static SingleItemPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SingleItemPresenter), new FrameworkPropertyMetadata(typeof(SingleItemPresenter)));
        }

        public bool IsLoadingIndicatorFadeInEnabled
        {
            get { return (bool)GetValue(IsLoadingIndicatorFadeInEnabledProperty); }
            set { SetValue(IsLoadingIndicatorFadeInEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingIndicatorFadeInEnabledProperty =
            DependencyProperty.Register(nameof(IsLoadingIndicatorFadeInEnabled), typeof(bool), typeof(SingleItemPresenter), new PropertyMetadata(false));

        public void RunNotificationAnimation(AnimationPurpose animationPurpose)
        {
            if (GetTemplateChild("PART_BorderToAnimate") is Border borderToAnimate)
            {
                TileAnimationHelper.RunAnimation(borderToAnimate, animationPurpose);
            }
        }

        public bool IsDimmed
        {
            get { return (bool)GetValue(IsDimmedProperty); }
            set { SetValue(IsDimmedProperty, value); }
        }

        public static readonly DependencyProperty IsDimmedProperty =
            DependencyProperty.Register(nameof(IsDimmed), typeof(bool), typeof(SingleItemPresenter), new PropertyMetadata(false));

        public double ThumbnailAreaMaxHeight
        {
            get { return (double)GetValue(ThumbnailAreaMaxHeightProperty); }
            set { SetValue(ThumbnailAreaMaxHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailAreaMaxHeightProperty =
            DependencyProperty.Register(nameof(ThumbnailAreaMaxHeight), typeof(double), typeof(SingleItemPresenter), new PropertyMetadata(0d));



        public double ThumbnailAreaMinHeight
        {
            get { return (double)GetValue(ThumbnailAreaMinHeightProperty); }
            set { SetValue(ThumbnailAreaMinHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailAreaMinHeightProperty =
            DependencyProperty.Register(nameof(ThumbnailAreaMinHeight), typeof(double), typeof(SingleItemPresenter), new PropertyMetadata(0d));



        public ClipboardViewItemWrapper ViewItemWrapper
        {
            get { return (ClipboardViewItemWrapper)GetValue(ViewItemWrapperProperty); }
            set { SetValue(ViewItemWrapperProperty, value); }
        }

        public static readonly DependencyProperty ViewItemWrapperProperty =
            DependencyProperty.Register(nameof(ViewItemWrapper), typeof(ClipboardViewItemWrapper), typeof(SingleItemPresenter), new PropertyMetadata(null));


    }


}
