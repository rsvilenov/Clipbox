using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Infrastructure.UI.Windows;
using System;
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

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;

            AssociatedObject.Unloaded += AssociatedObject_Unloaded;
            _isUnloadedEventWired = true;

            if (AssociatedObject.IsLoaded)
            {
                Setup();
                Scale();
            }
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsunscribeEventsIfSubscribed();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            
            Setup();
            Scale();
        }

        private void Setup()
        {
            _pmDpiwindow = Window.GetWindow(AssociatedObject) as PerMonitorDpiWindow;
            if (_pmDpiwindow == null)
            {
                throw new NotSupportedException($"{nameof(FrameworkElementDpiScalingBehavior)} can only be used with {nameof(PerMonitorDpiWindow)}");
            }

            _scaleFactor = _pmDpiwindow.ScaleFactor;

            _pmDpiwindow.DPIChanged += Wnd_DPIChanged;
            _isDpiEventWired = true;
        }

        private void Scale()
        {
            if (_pmDpiwindow.ScaleMode == Windows.Enums.DpiScaleMode.Custom)
            {
                Scaling.ScaleElement(_scaleFactor, AssociatedObject);
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
