using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Win32.Native;
using System;
using System.Windows;
using System.Windows.Interop;

namespace Clipbox.Views
{
    public static class LoadingPopupGoverner
    {
        public static FrameworkElement Parent { get; private set; }
        
        private static IntPtr _parentHwnd;

        private static LoadingIndicatorPopup _popup;

        private static OwnThreadDispatcher _ownThreadDispatcher;
        
        public static object _lockHandle = new object();

        private static bool _closeStarted = false;

        private static void SetParent(FrameworkElement parent)
        {
            if (Parent != null)
            {
                Parent.IsVisibleChanged -= Parent_IsVisibleChanged;
            }

            Parent = parent;
            _parentHwnd = new WindowInteropHelper(Window.GetWindow(Parent)).Handle;
            Parent.IsVisibleChanged += Parent_IsVisibleChanged;
        }

        public static void OpenPopup(FrameworkElement parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            SetParent(parent);

            Point locationFromScreen = Parent.PointToScreen(new Point(0, 0));
            Point parentSize = Parent.PointToScreen(new Point(Parent.ActualWidth, Parent.ActualHeight));

            // Transform screen point to WPF device independent point

            PresentationSource source = PresentationSource.FromVisual(Parent);

            Point targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
            Point parentSizePoint = source.CompositionTarget.TransformFromDevice.Transform(parentSize);

            _ownThreadDispatcher = new OwnThreadDispatcher();
            _ownThreadDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send,
                new Action(() =>
                {
                    _popup = new LoadingIndicatorPopup();

                    _popup.Closed += _popup_Closed;
                    _popup.SourceInitialized += _popup_SourceInitialized;
                    _popup.Show();
                }));
        }

        private static void _popup_SourceInitialized(object sender, EventArgs e)
        {
            _popup.SourceInitialized -= _popup_SourceInitialized;

            // do not let the window float freely
            User32.SetParent(_popup.Handle, _parentHwnd);
        }

        public static void ClosePopup(bool fadeOut = true)
        {
            if (Parent == null || _popup == null || _ownThreadDispatcher == null || _closeStarted)
            {
                return;
            }

            lock (_lockHandle)
            {
                if (Parent == null || _popup == null || _ownThreadDispatcher == null || _closeStarted)
                {
                    return;
                }

                _closeStarted = true;

                if (Parent != null)
                {
                    Parent.IsVisibleChanged -= Parent_IsVisibleChanged;
                }
            }

            _ownThreadDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send,
                new Action(() =>
                {
                    lock (_lockHandle)
                    {
                        if (fadeOut && Parent != null && Parent.IsVisible)
                        {
                            _popup.FadeOut();
                        }
                        else
                        {
                            _popup.Close();
                        }

                        Parent = null;
                    }
                }));
        }

        private static void _popup_Closed(object sender, EventArgs e)
        {
            lock (_lockHandle)
            {
                (sender as Window).Closed -= _popup_Closed;
                _ownThreadDispatcher.Dispose();
                _popup = null;
                _ownThreadDispatcher = null;

                _closeStarted = false;
            }
        }

        private static void Parent_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                ClosePopup(fadeOut: false);
            }
        }
    }
}
