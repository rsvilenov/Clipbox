using Clipbox.Views.Enums.ClipboardViewItemTile;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Clipbox.Views.ClipboardViewItemTile
{
    public static class TileAnimationHelper
    {
        public const int COLOR_ANIMATION_DURATION_MS = 300;

        public static void RunAnimation(Border borderToAnimate, AnimationPurpose animationPurpose)
        {
            ColorAnimation animation = new ColorAnimation();
            switch (animationPurpose)
            {
                case AnimationPurpose.Success:
                    animation.To = Colors.White;
                    break;
                case AnimationPurpose.Error:
                    animation.To = Colors.Red;
                    break;
            }

            animation.Duration = new Duration(TimeSpan.FromMilliseconds(COLOR_ANIMATION_DURATION_MS));
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Border.Background).(SolidColorBrush.Color)", null));
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.FillBehavior = FillBehavior.Stop;
            storyboard.Begin(borderToAnimate);
        }
    }
}
