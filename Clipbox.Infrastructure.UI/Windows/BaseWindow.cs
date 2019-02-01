using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Clipbox.Infrastructure.UI.Windows
{
    public class BaseWindow : Window
    {
        public IntPtr Handle { get; private set; }

        private HwndSource _hWndSource;
        
        public event EventHandler Shown;

        public BaseWindow()
        {
            SourceInitialized += BaseWindow_SourceInitialized;
            Closing += BaseWindow_Closing;
        }

        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hWndSource.Dispose();
        }

        private void BaseWindow_SourceInitialized(object sender, EventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;
            
            _hWndSource = HwndSource.FromHwnd(Handle);
            var hWndSourceHook = new HwndSourceHook(WndProc);
            _hWndSource.AddHook(hWndSourceHook);
        }

        protected virtual void OnSizeOrPositionChanging(ref System.Windows.Point position, ref System.Windows.Size size)
        {
        }

        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // https://docs.microsoft.com/en-us/windows/desktop/winmsg/wm-showwindow
            // only process the messages which are result of ShowWindow()
            // TODO: find out why we receive this message twice and upon processing of the first message, we get the old monitor from GetByWindowHandle.
            if (msg == NativeConstants.WmConstants.WM_SHOWWINDOW &&
                    (int)wParam == 1 &&
                    (int)lParam == 0)
            {
                Shown?.Invoke(this, new EventArgs());
            }
            else if (msg == NativeConstants.WmConstants.WM_GETMINMAXINFO)
            {
                var rectangle = WindowUtils.GetWindowRectangle(hwnd);
                Point position = new Point(rectangle.Top, rectangle.Left);
                Size size = new Size(rectangle.Size.Width, rectangle.Size.Height);
                OnSizeOrPositionChanging(ref position, ref size);

                NativeStructures.MINMAXINFO mmi = (NativeStructures.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(NativeStructures.MINMAXINFO));
                mmi.ptMaxPosition.X = (int)position.X;
                mmi.ptMaxPosition.Y = (int)position.Y;
                mmi.ptMaxSize.X = (int)size.Width;
                mmi.ptMaxSize.Y = (int)size.Height;

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }
    }
}
