using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Win32
{
    public class OsVersionUtils
    {
        private static WindowsVersion _windowsVersion;

        public static WindowsVersion GetWindowsVersion()
        {
            // It looks like Environment.OSVersion depends on GetVersionEx, which on its turn returns
            // different versions depending on how the application is manifested. However,
            // I do not want to rely on application manifest files, because they can be ignored,
            // if the app is 32 bit and it is targetting 64 bit windows, as shown here:
            // https://stackoverflow.com/questions/11705047/c-sharp-application-manifest-ignored-not-working
            // https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/fcebe7a4-2841-4898-9621-2e638de83fa6/application-manifest-may-be-ignored-in-windows-7-x64-ja?forum=windowscompatibility

            if (_windowsVersion != null)
            {
                return _windowsVersion;
            }

            using (RegistryKey key = Registry.LocalMachine
               .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                List<string> valueNames = key.GetValueNames().ToList();
                int majorVersionNumber = -1;
                int minorVersionNumber = -1;
                string buildNumber = null;
                int ubr = -1;
                string legacyVersionString = null;
                string editionId = null;

                if (valueNames.Contains("CurrentVersion"))
                {
                    legacyVersionString = key.GetValue("CurrentVersion")?.ToString();
                }

                if (valueNames.Contains("CurrentMajorVersionNumber"))
                {
                    majorVersionNumber = (int)key.GetValue("CurrentMajorVersionNumber");
                }

                if (valueNames.Contains("CurrentMinorVersionNumber"))
                {
                    minorVersionNumber = (int)key.GetValue("CurrentMinorVersionNumber");
                }

                if (valueNames.Contains("CurrentBuildNumber"))
                {
                    buildNumber = key.GetValue("CurrentBuildNumber")?.ToString();
                }
                else if (valueNames.Contains("CurrentBuild"))
                {
                    buildNumber = key.GetValue("CurrentBuild")?.ToString();
                }

                if (valueNames.Contains("EditionID"))
                {
                    editionId = key.GetValue("EditionID")?.ToString();
                }

                if (valueNames.Contains("UBR"))
                {
                    ubr = (int)key.GetValue("UBR");
                }

                _windowsVersion = new WindowsVersion
                {
                    BuildNumber = buildNumber,
                    EditionId = editionId,
                    LegacyVersionString = legacyVersionString,
                    MajorVersion = majorVersionNumber,
                    MinorVersion = minorVersionNumber,
                    UBR = ubr
                };

                return _windowsVersion;
            }
        }

        public static bool CheckIsWin81OrHigher()
        {
            WindowsVersion wv = GetWindowsVersion();
            return wv.LegacyVersionString == "6.3"
                || (wv.MajorVersion >= 6 && wv.MinorVersion >= 3);
        }
        
        public class WindowsVersion
        {
            public int MajorVersion { get; set; }
            public int MinorVersion { get; set; }

            /// <summary>
            /// The value of the BuildNumber in the registry is SZ,
            /// so there is no guarantee that it will always be parsable to int
            /// </summary>
            public string BuildNumber { get; set; }
            public int UBR { get; set; }
            public string EditionId { get; set; }
            public string LegacyVersionString { get; set; }
        }
    }
}
