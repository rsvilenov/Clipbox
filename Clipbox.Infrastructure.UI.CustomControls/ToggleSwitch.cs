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
    [TemplatePart(Name = "PART_Container", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_Thumb", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_ThumbMoveArea", Type = typeof(FrameworkElement))]
    public class ToggleSwitch : Control
    {
        private const int _thumbLeftRightMargin = 10;

        private bool _isDragging;
        private Point _clickPosition;
        private FrameworkElement _thumbMoveArea;
        private FrameworkElement _thumb;

        public override void OnApplyTemplate()
        {
            FrameworkElement container = this.Template.FindName("PART_ButtonContainer", this) as FrameworkElement;
            container.MouseLeftButtonUp += Grid_MouseLeftButtonUp;
            container.MouseLeftButtonDown += Grid_MouseLeftButtonDown;
            container.MouseMove += Grid_MouseMove;

            _thumb = this.Template.FindName("PART_Thumb", this) as FrameworkElement;
            
            _thumbMoveArea = this.Template.FindName("PART_ThumbMoveArea", this) as FrameworkElement;
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = true;
            _clickPosition = e.GetPosition(_thumbMoveArea);
            _thumb.CaptureMouse();
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && _thumb != null)
            {
                Point mousePos = e.GetPosition(_thumbMoveArea);

                double minX = _thumbLeftRightMargin;
                double maxX = ((double)_thumbMoveArea.ActualWidth - _thumb.ActualWidth - _thumbLeftRightMargin);

                if (mousePos.X > minX && mousePos.X < maxX)
                {
                    _thumb.SetValue(Canvas.LeftProperty, mousePos.X);
                }
            }
        }
        
        private void Grid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //IsChecked = !IsChecked;

            _isDragging = false;
            _thumb.ReleaseMouseCapture();
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
