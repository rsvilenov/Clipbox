using Clipbox.ViewModels.BindableObjects;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public abstract class ItemPresenterBase : Control
    {
        public Color PreviewContainerBackgroundColor
        {
            get { return (Color)GetValue(PreviewContainerBackgroundColorProperty); }
            set { SetValue(PreviewContainerBackgroundColorProperty, value); }
        }

        public static readonly DependencyProperty PreviewContainerBackgroundColorProperty =
            DependencyProperty.Register(nameof(PreviewContainerBackgroundColor), typeof(Color), typeof(ItemPresenterBase),
                /* the color should not be null, because we need the container to be hit-testable (for mouse events to fire) */
                new PropertyMetadata(Colors.Transparent));


        public bool IsFadeInEnabled
        {
            get { return (bool)GetValue(IsFadeInEnabledProperty); }
            set { SetValue(IsFadeInEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsFadeInEnabledProperty =
            DependencyProperty.Register(nameof(IsFadeInEnabled), typeof(bool), typeof(ItemPresenterBase), new PropertyMetadata(false));

        
        public bool IsLoadingFadeInEnabled
        {
            get { return (bool)GetValue(IsLoadingFadeInEnabledProperty); }
            set { SetValue(IsLoadingFadeInEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingFadeInEnabledProperty =
            DependencyProperty.Register(nameof(IsLoadingFadeInEnabled), typeof(bool), typeof(ItemPresenterBase), new PropertyMetadata(false));


        public bool IsLoadingIndicatorFadeInEnabled
        {
            get { return (bool)GetValue(IsLoadingIndicatorFadeInEnabledProperty); }
            set { SetValue(IsLoadingIndicatorFadeInEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingIndicatorFadeInEnabledProperty =
            DependencyProperty.Register(nameof(IsLoadingIndicatorFadeInEnabled), typeof(bool), typeof(ItemPresenterBase), new PropertyMetadata(false));


        public ClipboardViewItemWrapper ViewItemWrapper
        {
            get { return (ClipboardViewItemWrapper)GetValue(ViewItemWrapperProperty); }
            set { SetValue(ViewItemWrapperProperty, value); }
        }

        public static readonly DependencyProperty ViewItemWrapperProperty =
            DependencyProperty.Register(nameof(ViewItemWrapper), typeof(ClipboardViewItemWrapper), typeof(ItemPresenterBase), new PropertyMetadata(null, ViewItemWrapper_PropertyChanged));

        private static void ViewItemWrapper_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ItemPresenterBase)d).OnViewItemWrapperPropertyChanged(e.NewValue as ClipboardViewItemWrapper);
        }

        protected virtual void OnViewItemWrapperPropertyChanged(ClipboardViewItemWrapper newValue) { }
    }
}
