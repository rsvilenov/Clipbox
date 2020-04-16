using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Clipbox.Infrastructure.UI.CustomControls
{
    [TemplatePart(Name = "PART_ThumbContainer", Type = typeof(Border))]
    [TemplatePart(Name = "PART_Thumb", Type = typeof(Ellipse))]
    public class ToggleSwitch : Control
    {

        public override void OnApplyTemplate()
        {
            Border border = this.Template.FindName("PART_ThumbContainer", this) as Border;
            border.MouseLeftButtonUp += Border_MouseLeftButtonUp;

        }

        private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(ToggleSwitch), new PropertyMetadata(true));



        public string OnText
        {
            get { return (string)GetValue(OnTextProperty); }
            set { SetValue(OnTextProperty, value); }
        }

        public static readonly DependencyProperty OnTextProperty =
            DependencyProperty.Register(nameof(OnText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata(string.Empty));
        

        public string OffText
        {
            get { return (string)GetValue(OffTextProperty); }
            set { SetValue(OffTextProperty, value); }
        }

        public static readonly DependencyProperty OffTextProperty =
            DependencyProperty.Register(nameof(OffText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata(string.Empty));



        public Brush OffForeground
        {
            get { return (Brush)GetValue(OffForegroundProperty); }
            set { SetValue(OffForegroundProperty, value); }
        }

        public static readonly DependencyProperty OffForegroundProperty =
            DependencyProperty.Register(nameof(OffForeground), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.LightGray));



        public Brush OffBackground
        {
            get { return (Brush)GetValue(OffBackgroundProperty); }
            set { SetValue(OffBackgroundProperty, value); }
        }
        
        public static readonly DependencyProperty OffBackgroundProperty =
            DependencyProperty.Register(nameof(OffBackground), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Black));



    }
}
