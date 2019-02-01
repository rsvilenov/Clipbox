using System;
using System.Runtime.InteropServices;
using static Clipbox.Win32.Native.NativeEnums;

namespace Clipbox.Win32.Native
{
    public static class SHCore
    {
        [DllImport(ExternDll.SHCore, SetLastError = true)]
        internal static extern int GetProcessDpiAwareness(IntPtr hprocess, out ProcessDPIAwareness awareness);

        [DllImport(ExternDll.SHCore, SetLastError = true)]
        internal static extern int SetProcessDpiAwareness(ProcessDPIAwareness value);
        
        [DllImport(ExternDll.SHCore, SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor,
                                        MONITOR_DPI_TYPE dpiType,
                                        out uint dpiX,
                                        out uint dpiY);
    }
}
