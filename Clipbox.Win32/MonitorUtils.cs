using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using static Clipbox.Win32.Native.NativeStructures;

namespace Clipbox.Win32
{
    public static class MonitorUtils
    {
        public class DisplayMonitor
        {
            public IntPtr Handle;
            public double Dpi;
            public Rectangle Bounds;
            public Rectangle WorkingArea;
        }

        public static DisplayMonitor GetByCurrentMousePosition()
        {
            Point p = new Point();

            if (!User32.GetCursorPos(ref p))
            {
                throw new Win32Exception();
            }

            IntPtr hMonitor = User32.MonitorFromPoint(p, NativeEnums.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            
            return GetDisplayMonitor(hMonitor);
        }

        public static DisplayMonitor GetPrimaryMonitor()
        {
            // https://devblogs.microsoft.com/oldnewthing/20070809-00/?p=25643

            IntPtr hMonitor = User32.MonitorFromPoint(new Point(0, 0), NativeEnums.MonitorOptions.MONITOR_DEFAULTTONEAREST);

            return GetDisplayMonitor(hMonitor);
        }

        public static DisplayMonitor GetByWindowHandle(IntPtr hWnd)
        {
            var hMonitor = User32.MonitorFromWindow(hWnd, NativeConstants.MonitorConstants.MONITOR_DEFAULTTONEAREST);
            return GetDisplayMonitor(hMonitor);
        }

        public static DisplayMonitor GetByForegroundWindow()
        {
            IntPtr foregroundWindowHwnd = User32.GetForegroundWindow();
            return GetByWindowHandle(foregroundWindowHwnd);
        }

        public static IEnumerable<DisplayMonitor> ListDisplayMonitors()
        {
            List<DisplayMonitor> monitors = new List<DisplayMonitor>();

            User32.EnumDisplayMonitors(IntPtr.Zero, 
                IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeStructures.RECT lprcMonitor, IntPtr dwData)
                {
                    monitors.Add(GetDisplayMonitor(hMonitor));
                    return true;
                }, 
                IntPtr.Zero);

            return monitors;
        }

        private static DisplayMonitor GetDisplayMonitor(IntPtr hMonitor)
        {
            var dpi = DpiUtils.GetDPIForScreen(hMonitor);

            NativeStructures.MonitorInfoEx monInfo = new NativeStructures.MonitorInfoEx();
            monInfo.Init();
            monInfo.Size = (uint)Marshal.SizeOf(monInfo);
            bool success = User32.GetMonitorInfo(hMonitor, ref monInfo);
            if (!success)
            {
                MarshalExt.ThrowOnWin32Error();
            }

            return new DisplayMonitor
            {
                Handle = hMonitor,
                Dpi = dpi,
                Bounds = monInfo.Monitor.ToRectangle(),
                WorkingArea = monInfo.WorkArea.ToRectangle()
            };
        }
    }
}
