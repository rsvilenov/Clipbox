using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Clipbpx.SetUpDownloader
{
    public static class EnvironmentUtils
    {

        public static bool IsWindows10()
        {
            // in order for this to work properly, the windows 10 guid line in app.manifest must be present
            return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major == 10;
        }

        public static bool Is64BitOs()
        {
            if (IntPtr.Size == 4)
            {
                // works with .NET < 4.0
                using (Process p = Process.GetCurrentProcess())
                {
                    if (!IsWow64Process(p.Handle, out bool retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return true;
            }
        }


        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );
    }
}
