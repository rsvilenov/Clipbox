using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Clipbox.Win32
{
    public static class DpiUtils
    {
        /// <summary>
        /// Gets the current DPI of the monitor which is showing the window
        /// </summary>
        /// <param name="hWnd">A handle of the window</param>
        /// <returns></returns>
        public static double GetDPIForWindow(IntPtr hWnd)
        {
            var hMonitor = User32.MonitorFromWindow(hWnd, NativeConstants.MonitorConstants.MONITOR_DEFAULTTONEAREST);
            return GetDPIForScreen(hMonitor);
        }

        /// <summary>
        /// Make sure that the DPI Awareness of the process is set to PerMonitorDPI.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        /// <exception cref="InvalidOperationException">DPI awareness setting is not ProcessPerMonitorDPIAware.</exception>
        public static void AssertPerMonitorDPIAwareness()
        {
            // TODO: make this windows 7 compatible
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new PlatformNotSupportedException();
            }

            NativeEnums.ProcessDPIAwareness awareness = GetCurrentDPIAwareness();
            if (awareness != NativeEnums.ProcessDPIAwareness.ProcessPerMonitorDPIAware)
            {
                throw new InvalidOperationException("DPI awareness setting is not ProcessPerMonitorDPIAware. Please edit your app.manifest.");
            }
        }

        public static void EnsurePerMonitorDPIAwareness()
        {
            // TODO: make this windows 7 compatible
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new PlatformNotSupportedException();
            }

            NativeEnums.ProcessDPIAwareness awareness = GetCurrentDPIAwareness();
            if (awareness == NativeEnums.ProcessDPIAwareness.ProcessSystemDPIAware)
            {
                throw new InvalidOperationException("DPI awareness setting of ProcessSystemDPIAware (the default for WPF apps) was detected. However, we need ProcessPerMonitorDPIAware. Please edit your AssemblyInfo.cs and add the following line there: [assembly: DisableDpiAwareness].");
            }
            else if (awareness == NativeEnums.ProcessDPIAwareness.ProcessDPIUnaware)
            {
                int result = SHCore.SetProcessDpiAwareness(NativeEnums.ProcessDPIAwareness.ProcessPerMonitorDPIAware);
                if (result != 0)
                {
                    throw new Win32Exception("SetProcessDpiAwareness failed. Please do not call this method twice and ensure that there is the following line in AssemblyInfo.cs: [assembly: DisableDpiAwareness].");
                }
            }

            awareness = GetCurrentDPIAwareness();
            if (awareness == NativeEnums.ProcessDPIAwareness.ProcessDPIUnaware)
            {
                throw new Exception("Could not enter ProcessPerMonitorDPIAware mode.");
            }

        }

        public static NativeEnums.ProcessDPIAwareness GetCurrentDPIAwareness()
        {
            NativeEnums.ProcessDPIAwareness awareness;

            using (var currentProcess = Process.GetCurrentProcess())
            {
                int returnCode = SHCore.GetProcessDpiAwareness(currentProcess.Handle, out awareness);
                if (returnCode != 0 /* S_OK */)
                {
                    throw Marshal.GetExceptionForHR(returnCode);
                }
            }

            return awareness;
        }

        public static double GetDPIForScreen(IntPtr hMonitor, bool getAngularDpi = false)
        {
            uint dpiX = 0;
            uint dpiY = 0;
            var hResult = SHCore.GetDpiForMonitor(hMonitor, getAngularDpi ? NativeEnums.MONITOR_DPI_TYPE.MDT_ANGULAR_DPI : NativeEnums.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
            if (hResult != 0)
            {
                dpiX = 96;
            }

            return dpiX;
        }

    }
}
