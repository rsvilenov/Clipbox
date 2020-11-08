using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Infrastructure.UI.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interactivity;

namespace Clipbox.Infrastructure.UI.Behaviors
{
    /// <summary>
    /// A behavior for handling DpiScaleMode.Custom
    /// </summary>
    public class FrameworkElementDpiScalingBehavior : Behavior<FrameworkElement>
    {
        private double _scaleFactor;

        private PerMonitorDpiWindow _pmDpiwindow;

        private bool _isDpiEventWired;
        private bool _isUnloadedEventWired;

        
        public double MaxScaleFactor
        {
            get { return (double)GetValue(MaxScaleFactorProperty); }
            set { SetValue(MaxScaleFactorProperty, value); }
        }

        public static readonly DependencyProperty MaxScaleFactorProperty =
            DependencyProperty.Register(nameof(MaxScaleFactor), typeof(double), typeof(FrameworkElementDpiScalingBehavior), new PropertyMetadata(0D));
        
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;

            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            _isUnloadedEventWired = true;

            if (AssociatedObject.IsLoaded)
            {
                if (Setup())
                {
                    Scale();
                }
            }
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsunscribeEventsIfSubscribed();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;

            if (Setup())
            {
                Scale();
            }
        }

        private bool Setup()
        {
            Window wnd = Window.GetWindow(AssociatedObject);
            if (wnd == null)
            {
                Debug.WriteLine($"{nameof(FrameworkElementDpiScalingBehavior)}: the associated window cound not be resolved.");
                return false;

            }
            _pmDpiwindow = wnd as PerMonitorDpiWindow;
            if (_pmDpiwindow == null)
            {
                Debug.WriteLine($"{nameof(FrameworkElementDpiScalingBehavior)} can only be used with {nameof(PerMonitorDpiWindow)}");
                return false;
            }

            _scaleFactor = _pmDpiwindow.ScaleFactor;

            _pmDpiwindow.DPIChanged += Wnd_DPIChanged;
            _isDpiEventWired = true;

            return true;
        }

        private void Scale()
        {
            if (_pmDpiwindow.ScaleMode == Windows.Enums.DpiScaleMode.Custom)
            {
                double scaleFactor = MaxScaleFactor > 0 && _scaleFactor > MaxScaleFactor ?
                    MaxScaleFactor : _scaleFactor;

                Scaling.ScaleElement(scaleFactor, AssociatedObject);
            }
        }

        private void Wnd_DPIChanged(object sender, DpiChangedEventArgs e)
        {
            _scaleFactor = e.ScaleFactor;
            Scale();
        }

        protected override void OnDetaching()
        {
            UnsunscribeEventsIfSubscribed();
        }

        private void UnsunscribeEventsIfSubscribed()
        {
            if (_isDpiEventWired)
            {
                _pmDpiwindow.DPIChanged -= Wnd_DPIChanged;
            }

            if (_isUnloadedEventWired)
            {
                AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
                _isUnloadedEventWired = false;
            }
        }
    }
}
