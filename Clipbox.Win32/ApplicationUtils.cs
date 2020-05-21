using Clipbox.Win32.Native;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Clipbox.Win32
{
    public class ApplicationUtils
    {
        public class InstalledAppInfo
        {
            public string InstallLocation { get; internal set; }

            public string DisplayName { get; internal set; }

            public string DisplayIconPath { get; internal set; }

            public Icon DisplayIcon { get; internal set; }
        }

        public static List<InstalledAppInfo> GetAllInstalledApplications()
        {
            List<InstalledAppInfo> installedApps = new List<InstalledAppInfo>();
            installedApps.AddRange(CollectInstalledApplications(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"));
            installedApps.AddRange(CollectInstalledApplications(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"));

            if (Environment.Is64BitOperatingSystem)
            {
                if (IntPtr.Size == 4)
                {
                    throw new PlatformNotSupportedException("Please compile this program for 64 bit architecure.");
                }

                installedApps.AddRange(CollectInstalledApplications(Registry.CurrentUser, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
                installedApps.AddRange(CollectInstalledApplications(Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
            }

            installedApps.AddRange(CollectFromAppPaths(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths"));

            return installedApps
                .GroupBy(ia => ia.DisplayName)
                .Select(g => g.First())
                .ToList();
        }

        private static IEnumerable<InstalledAppInfo> CollectFromAppPaths(RegistryKey root, string path)
        {
            List<InstalledAppInfo> installedApps = new List<InstalledAppInfo>();

            using (RegistryKey rootKey = root.OpenSubKey(path))
            {
                if (rootKey != null)
                {
                    var subKeysNames = rootKey.GetSubKeyNames();
                    foreach (string subKeyName in subKeysNames)
                    {
                        using (RegistryKey subKey = rootKey.OpenSubKey(subKeyName))
                        {
                            InstalledAppInfo installedAppInfo = null;

                            string exePath = (string)subKey.GetValue("");
                            if (!string.IsNullOrWhiteSpace(exePath))
                            {
                                exePath = exePath.TrimStart('\"').TrimEnd('\"');
                                bool pathExists = false;
                                try
                                {
                                    pathExists = File.Exists(exePath);
                                }
                                catch { }

                                if (pathExists)
                                {
                                    installedAppInfo = GetAppInfoFromPath(exePath);
                                }
                            }


                            if (installedAppInfo != null
                                && !string.IsNullOrWhiteSpace(installedAppInfo.DisplayName)
                                && !string.IsNullOrWhiteSpace(installedAppInfo.InstallLocation)
                                && !installedApps.Any(ia => ia.InstallLocation == installedAppInfo.InstallLocation))
                            {
                                installedApps.Add(installedAppInfo);
                            }

                            subKey.Close();
                        }

                    }
                    rootKey.Close();
                }

            }

            return installedApps;
        }

        public static InstalledAppInfo GetAppInfoFromPath(string exePath)
        {
            InstalledAppInfo installedAppInfo = new InstalledAppInfo();

            installedAppInfo.DisplayIcon = ExtractIconFromModule(exePath);
            installedAppInfo.InstallLocation = Path.GetDirectoryName(exePath);
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(exePath);
            installedAppInfo.DisplayName = fileVersionInfo.FileDescription;

            if (exePath.Contains("\\WindowsApps\\") && fileVersionInfo.FileDescription != fileVersionInfo.ProductName)
            {
                installedAppInfo.DisplayName += " (" + fileVersionInfo.ProductName + ")";
            }

            return installedAppInfo;
        }

        private static IEnumerable<InstalledAppInfo> CollectInstalledApplications(RegistryKey root, string path)
        {
            using (RegistryKey rootKey = root.OpenSubKey(path))
            {
                if (rootKey != null)
                {
                    var subKeysNames = rootKey.GetSubKeyNames();
                    foreach (string subKeyName in subKeysNames)
                    {
                        using (RegistryKey subKey = rootKey.OpenSubKey(subKeyName))
                        {
                            string[] valueNames = subKey.GetValueNames();
                            var installedAppInfo = new InstalledAppInfo();

                            if (valueNames.Contains("DisplayName"))
                            {
                                installedAppInfo.DisplayName = (string)subKey.GetValue("DisplayName");
                            }
                            if (valueNames.Contains("DisplayIcon"))
                            {
                                installedAppInfo.DisplayIconPath = (string)subKey.GetValue("DisplayIcon");
                                if (!string.IsNullOrEmpty(installedAppInfo.DisplayIconPath))
                                {
                                    if (installedAppInfo.DisplayIconPath.EndsWith(".ico"))
                                    {
                                        installedAppInfo.DisplayIcon = new Icon(installedAppInfo.DisplayIconPath);
                                    }
                                    else
                                    {
                                        int iconIndex = 0;
                                        string[] iconParts = installedAppInfo.DisplayIconPath.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (iconParts.Length > 1)
                                        {
                                            iconIndex = int.Parse(iconParts[1].Trim());
                                        }

                                        string modulePath = iconParts[0].TrimStart('\"').TrimEnd('\"').Trim();

                                        bool pathExists = false;
                                        try
                                        {
                                            pathExists = File.Exists(modulePath);
                                        }
                                        catch { }

                                        if (pathExists)
                                        {
                                            installedAppInfo.DisplayIcon = ExtractIconFromModule(modulePath, iconIndex);
                                        }
                                    }
                                }
                            }
                            if (valueNames.Contains("InstallLocation"))
                            {
                                installedAppInfo.InstallLocation = (string)subKey.GetValue("InstallLocation");
                                if (string.IsNullOrWhiteSpace(installedAppInfo.InstallLocation)
                                    && !string.IsNullOrWhiteSpace(installedAppInfo.DisplayIconPath))
                                {
                                    installedAppInfo.InstallLocation = Path.GetDirectoryName(installedAppInfo.DisplayIconPath);
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(installedAppInfo.DisplayName) && !string.IsNullOrWhiteSpace(installedAppInfo.InstallLocation))
                            {
                                yield return installedAppInfo;
                            }

                            subKey.Close();
                        }

                    }
                    rootKey.Close();
                }

            }
        }

        private static Icon ExtractIconFromModule(string path, int iconIndex = 0)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"{path} does not exist.");
            }

            Icon icon = null;

            IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
            IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };

            int readIconCount = Shell32.ExtractIconEx(path, iconIndex, hIconEx, hDummy, 1);

            if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
            {
                icon = (Icon)Icon.FromHandle(hIconEx[0]).Clone();
            }

            return icon;
        }
    }
}
