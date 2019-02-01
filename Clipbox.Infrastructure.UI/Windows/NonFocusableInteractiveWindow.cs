using Clipbox.Infrastructure.UI.Theme.Enums;
using Clipbox.Win32;
using Clipbox.Win32.LLHooks;
using Clipbox.Win32.Native;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Windows
{
    public sealed class NonFocusableInteractiveWindow : AcryllicWindow
    {
        // TODO: can we use a simpler container here?
        private Grid _mainGrid;

        private Brush _containerBackground;

        private MouseLLHook _mouseHook;

        public Brush ContainerBackground
        {
            get
            {
                return _containerBackground;
            }
            set
            {
                _containerBackground = value;
                _mainGrid.Background = _containerBackground;
            }
        }

        private UIElement _content;

        public new UIElement Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
                _mainGrid.Children.Clear();
                _mainGrid.Children.Add(_content);
            }
        }

        public NonFocusableInteractiveWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            Focusable = false;
            ShowActivated = false;

            Width = 500;
            Height = 300;

            _containerBackground = TryFindResource(ThemeBrush.TaskbarColorBrush) as Brush ?? new SolidColorBrush(Colors.Black);
            _containerBackground.Opacity = 0.8;

            _mainGrid = new Grid();
            _mainGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            _mainGrid.VerticalAlignment = VerticalAlignment.Stretch;
            _mainGrid.Background = _containerBackground;

            base.Content = _mainGrid;

            _mouseHook = new MouseLLHook();
            _mouseHook.MouseAction += _mouseHook_MouseAction;

            IsVisibleChanged += NonFocusableInteractiveWindow_IsVisibleChanged;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            
            SourceInitialized += NonFocusableInteractiveWindow_SourceInitialized;
        }

        private void NonFocusableInteractiveWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                _mouseHook.Start();
            }
            else
            {
                _mouseHook.Stop();
            }
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            _mouseHook.Dispose();
        }
        
        private void _mouseHook_MouseAction(MouseEventArgs args)
        {
            if (args.MouseAction == Win32.Native.NativeEnums.MouseMessages.WM_LBUTTONDOWN)
            {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(() =>
                {
                    if (this.IsVisible)
                    {
                        Debug.WriteLine("MouseHookAction invoked");
                        Point mouseClickPoint = new Point(args.Point.X, args.Point.Y);
                        Point wndPosition = this.PointToScreen(new Point(0, 0));
                        if ((new Rect(wndPosition, this.RenderSize)).Contains(mouseClickPoint))
                        {
                            //Utils.Automation.RaiseWpfClickEventsAtScreenPosition(args.Point.X, args.Point.Y);
                            Utils.Automation.DoClickOperationsUnderMouse(this);
                            args.CancelEvent = true;
                        }
                    }
                }));
            }
        }

        private void NonFocusableInteractiveWindow_SourceInitialized(object sender, System.EventArgs e)
        {
        //    var source = PresentationSource.FromVisual(this) as HwndSource;
        //    source.AddHook(WndProc);
            
            WindowUtils.AddWindowStyleEx(base.Handle, Win32.Native.NativeEnums.WindowStylesEx.WS_EX_TOOLWINDOW);
        }



        private const int MA_NOACTIVATE = 0x0003;

        // TODO: We are already using a hook to stop mouse clicks from reaching the window. Do we need to do this as well?
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Disable the activation of the window upon mouse click
            if (msg == (int)NativeConstants.WmConstants.WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }

            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }
    }
}
