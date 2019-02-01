using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Clipbox.Infrastructure.UI.Animations
{
    public static class WindowBoundsAnimation
    {
        public static void Animate(Window window, Rect bounds, int durationMs)
        {
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                From = window.ActualWidth,
                To = bounds.Width,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            DoubleAnimation heightAnimation = new DoubleAnimation
            {
                From = window.ActualHeight,
                To = bounds.Height,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            DoubleAnimation topAnimation = new DoubleAnimation
            {
                From = double.IsNaN(window.Top) ? 0d : window.Top,
                To = bounds.Top,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            DoubleAnimation leftAnimation = new DoubleAnimation
            {
                From = double.IsNaN(window.Left) ? 0d : window.Left,
                To = bounds.Left,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Window.WidthProperty));
            Storyboard.SetTarget(widthAnimation, window);

            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(Window.HeightProperty));
            Storyboard.SetTarget(heightAnimation, window);

            Storyboard.SetTargetProperty(topAnimation, new PropertyPath(Window.TopProperty));
            Storyboard.SetTarget(topAnimation, window);

            Storyboard.SetTargetProperty(leftAnimation, new PropertyPath(Window.LeftProperty));
            Storyboard.SetTarget(leftAnimation, window);

            Storyboard s = new Storyboard();
            s.Children.Add(widthAnimation);
            s.Children.Add(heightAnimation);
            s.Children.Add(topAnimation);
            s.Children.Add(leftAnimation);
            s.Begin();
        }

    }
}
