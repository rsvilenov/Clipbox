using Microsoft.Win32;
using System;
using System.Globalization;

namespace Clipbox.Win32
{
    public class OsVersionUtils
    {
        private static double _windowsVersion;

        public static double GetWindowsVersion()
        {
            // It looks like Environment.OSVersion depends on GetVersionEx, which on its turn returns
            // different versions depending on how the application is manifested. However,
            // I do not want to rely on application manifest files, because they can be ignored,
            // if the app is 32 bit and it is targetting 64 bit windows, as shown here:
            // https://stackoverflow.com/questions/11705047/c-sharp-application-manifest-ignored-not-working
            // https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/fcebe7a4-2841-4898-9621-2e638de83fa6/application-manifest-may-be-ignored-in-windows-7-x64-ja?forum=windowscompatibility

            if (_windowsVersion != 0)
            {
                return _windowsVersion;
            }

            using (RegistryKey key = Registry.LocalMachine
               .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                var osVersion = key.GetValue("CurrentVersion");
                double version = 0d;
                if (osVersion != null && double.TryParse(osVersion.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out version))
                {
                    _windowsVersion = version;
                    return version;
                }
                else
                {
                    throw new Exception("Unable to determine the version of Windows.");
                }
            }
        }

        public static bool CheckIsWin81OrHigher()
        {
            return GetWindowsVersion() >= 6.3;
        }
        
    }
}
