using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Drawing;

namespace Clipbox.Views
{
    public class WallpaperWindow : GdiCanvasWindow
    {
        private MonitorUtils.DisplayMonitor _monitor;

        public WallpaperWindow(MonitorUtils.DisplayMonitor monitor)
        {
            _monitor = monitor;
        }

        protected override void OnCreateing()
        {
            // remove the caption
            WindowUtils.RemoveWindowStyle(Handle, NativeEnums.WindowStyles.WS_CAPTION);

            // hide from alt+tab
            WindowUtils.AddWindowStyleEx(Handle, NativeEnums.WindowStylesEx.WS_EX_TOOLWINDOW);

            // bring the window on top
            User32.SetForegroundWindow(Handle);

            Maximize();
        }

        protected override void OnPainting(Graphics g)
        {
            IntPtr hdc = g.GetHdc();

            try
            {
                // this seems to be the fastest and most memory efficient way
                // to draw the desktop background image on the window...
                if (!User32.PaintDesktop(hdc))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        protected override void OnSizeOrPositionChanging(ref Point position, ref Size size)
        {
            // when there are no window styles set the maximized window covers the taskbar
            // and we do not want that, so we have to do the following:

            // https://blogs.msdn.microsoft.com/llobo/2006/08/01/maximizing-window-with-windowstylenone-considering-taskbar/

            position.X = Math.Abs(_monitor.WorkingArea.Left - _monitor.Bounds.Left);
            position.Y = Math.Abs(_monitor.WorkingArea.Top - _monitor.Bounds.Top);
            size.Width = Math.Abs(_monitor.WorkingArea.Right - _monitor.Bounds.Left);
            size.Height = Math.Abs(_monitor.WorkingArea.Bottom - _monitor.Bounds.Top);
        }
    }
}
