using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.CustomControls
{
    [TemplatePart(Name = "PART_ButtonContainer", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_Thumb", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_ThumbMoveArea", Type = typeof(Canvas))]
    public class ToggleSwitch : Control
    {
        private const int THUMB_LEFT_RIGHT_MARGIN = 10;
        private const int SWITCH_ANIMATION_DURATION_MS = 200;

        private bool _isDragging;
        private Point _clickPosition;
        private Canvas _thumbMoveArea;
        private FrameworkElement _thumb;

        private readonly ManualResetEvent _animationCompletedMre;

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        }

        public ToggleSwitch()
        {
            _animationCompletedMre = new ManualResetEvent(true);
        }

        public override void OnApplyTemplate()
        {
            Loaded += ToggleSwitch_Loaded;

            FrameworkElement container = this.Template.FindName("PART_ButtonContainer", this) as FrameworkElement;
            if (container == null)
            {
                throw new InvalidOperationException("PART_ButtonContainer is not present in the template or it is not a FrameworkElement.");
            }

            container.MouseLeftButtonUp += Grid_MouseLeftButtonUp;
            container.MouseLeftButtonDown += Grid_MouseLeftButtonDown;
            container.MouseMove += Grid_MouseMove;

            _thumb = this.Template.FindName("PART_Thumb", this) as FrameworkElement;
            if (_thumb == null)
            {
                throw new InvalidOperationException("PART_Thumb is not present in the template or it is not a FrameworkElement.");
            }

            _thumbMoveArea = this.Template.FindName("PART_ThumbMoveArea", this) as Canvas;
            if (_thumbMoveArea == null)
            {
                throw new InvalidOperationException("PART_ThumbMoveArea is not present in the template or it is not a Canvas.");
            }

            IsInteractionReady = true;
        }

        private void ToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            SyncTogglePositionWithBtnValue();
        }

        private void SyncTogglePositionWithBtnValue()
        {
            if (!IsInteractionReady)
            {
                return;
            }

            if (IsChecked)
            {
                SetThumbToOnPosition(useAnimation: false);
            }
            else
            {
                SetThumbToOffPosition(useAnimation: false);
            }
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsInteractionReady)
            {
                return;
            }

            _isDragging = true;
            _clickPosition = e.GetPosition(_thumbMoveArea);
            _thumb.CaptureMouse();
            IsToggling = true;
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsInteractionReady && _isDragging)
            {
                Point mousePos = e.GetPosition(_thumbMoveArea);

                // Grid_MouseMove fires immediately after Grid_MouseLeftButtonDown
                // regardless of whether the mouse has been moved; so in order to
                // prevent jumpy behaviour of the thumb when the user clicks
                // somewhere in the middle of the ToggleSwitch, we have to
                // check whether the mouse has actually been moved.
                if (mousePos == _clickPosition)
                {
                    return;
                }

                double minX = GetThumbFinalOffXPosInCanvas();
                double maxX = GetThumbFinalOnXPosInCanvas();

                if (mousePos.X > minX && mousePos.X < maxX)
                {
                    _thumb.SetValue(Canvas.LeftProperty, mousePos.X);
                }
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsInteractionReady)
            {
                return;
            }

            _isDragging = false;

            _thumb.ReleaseMouseCapture();

            double currentThumbXPos = GetThumbCurrentXPosInCanvas();
            double finalOnXPos = GetThumbFinalOnXPosInCanvas();
            double finalOffXPos = GetThumbFinalOffXPosInCanvas();

            // if the user clicks on the control and does not move
            // the mouse, just toggle the state of IsChecked
            if (currentThumbXPos == finalOnXPos && IsChecked)
            {
                SetThumbToOffPosition(useAnimation: true);
                IsChecked = false;
            }
            else if (currentThumbXPos == finalOffXPos && !IsChecked)
            {
                SetThumbToOnPosition(useAnimation: true);
                IsChecked = true;
            }
            else
            {
                // check whether the current position is closer to Off than to On
                // or vice versa, and set the IsChecked state accordingly.
                if (finalOnXPos - currentThumbXPos > currentThumbXPos - finalOffXPos)
                {
                    SetThumbToOffPosition(useAnimation: true);
                    IsChecked = false;
                }
                else
                {
                    SetThumbToOnPosition(useAnimation: true);
                    IsChecked = true;
                }
            }

            IsToggling = false;
        }

        private double GetThumbFinalOffXPosInCanvas()
        {
            return THUMB_LEFT_RIGHT_MARGIN;
        }

        private double GetThumbFinalOnXPosInCanvas()
        {
            return ((double)_thumbMoveArea.ActualWidth - _thumb.ActualWidth - THUMB_LEFT_RIGHT_MARGIN);
        }

        private double GetThumbCurrentXPosInCanvas()
        {
            double pos = (double)_thumb.GetValue(Canvas.LeftProperty);
            return pos == double.NaN ? 0 : pos;
        }

        private void SetThumbToOffPosition(bool useAnimation)
        {
            Task.Run(() =>
            {
                _animationCompletedMre.WaitOne();

                double finalThumbOffXPos = GetThumbFinalOffXPosInCanvas();
                Dispatcher.Invoke(() =>
                {
                    if (useAnimation)
                    {
                        AnimateThumbToPosition(finalThumbOffXPos);
                    }
                    else
                    {
                        SetThumbPosition(finalThumbOffXPos);
                    }
                }, DispatcherPriority.Send);
            });
        }

        private void SetThumbToOnPosition(bool useAnimation)
        {
            Task.Run(() =>
                {
                    _animationCompletedMre.WaitOne();

                    double finalThumOnPosX = GetThumbFinalOnXPosInCanvas();
                    Dispatcher.Invoke(() =>
                    {
                        if (useAnimation)
                        {
                            AnimateThumbToPosition(finalThumOnPosX);
                        }
                        else
                        {
                            SetThumbPosition(finalThumOnPosX);
                        }
                    }, DispatcherPriority.Send);
                });
        }


        private void SetThumbPosition(double position)
        {
            _animationCompletedMre.Reset();
            Dispatcher.Invoke(() =>
            {
                _thumb.SetValue(Canvas.LeftProperty, position);
                _animationCompletedMre.Set();
            });
        }

        private void AnimateThumbToPosition(double position)
        {
            _animationCompletedMre.Reset();
            Dispatcher.Invoke(() =>
            {
                double currentThumbXPos = GetThumbCurrentXPosInCanvas();
                DoubleAnimation anim = new DoubleAnimation(currentThumbXPos, position, (Duration)TimeSpan.FromMilliseconds(SWITCH_ANIMATION_DURATION_MS));

                    // remove the animation from the property once it completes,
                    // so we can set the property's value from elsewhere.
                    // (the property is 'blocked' if there is an animation on it)
                    anim.Completed += (e, args) =>
                {
                    _thumb.BeginAnimation(Canvas.LeftProperty, null);
                    _thumb.SetValue(Canvas.LeftProperty, position);

                    _animationCompletedMre.Set();
                };

                _thumb.BeginAnimation(Canvas.LeftProperty, anim);
            });
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(ToggleSwitch), new PropertyMetadata(false, IsChecked_PropertyChanged));

        private static void IsChecked_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ToggleSwitch ts = (ToggleSwitch)d;
            ts.Dispatcher.Invoke(() => ts.SyncTogglePositionWithBtnValue(), DispatcherPriority.ContextIdle);
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

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
        

        public bool IsToggling
        {
            get { return (bool)GetValue(IsTogglingProperty); }
            set { SetValue(IsTogglingProperty, value); }
        }

        public static readonly DependencyProperty IsTogglingProperty =
            DependencyProperty.Register(nameof(IsToggling), typeof(bool), typeof(ToggleSwitch), new PropertyMetadata(false));


        public bool IsInteractionReady
        {
            get { return (bool)GetValue(IsInteractionReadyProperty); }
            set { SetValue(IsInteractionReadyProperty, value); }
        }

        public static readonly DependencyProperty IsInteractionReadyProperty =
            DependencyProperty.Register(nameof(IsInteractionReady), typeof(bool), typeof(ToggleSwitch), new PropertyMetadata(false));

    }
}
