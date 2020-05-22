using Clipbox.Win32.Native;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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

        public static Bitmap GetModernAppIcon(string appFullPath)
        {
            // https://stackoverflow.com/questions/32122679/getting-icon-of-modern-windows-app-from-a-desktop-application

            var dir = System.IO.Path.GetDirectoryName(appFullPath);
            var manifestPath = System.IO.Path.Combine(dir, "AppxManifest.xml");
            if (File.Exists(manifestPath))
            {
                // this is manifest file
                string pathToLogo;
                using (var fs = File.OpenRead(manifestPath))
                {
                    var manifest = XDocument.Load(fs);
                    const string ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                    // rude parsing - take more care here
                    try
                    {
                        pathToLogo = manifest.Root.Element(XName.Get("Properties", ns)).Element(XName.Get("Logo", ns)).Value;
                    }
                    catch
                    {
                        return null;
                    }
                }
                // now here it is tricky again - there are several files that match logo, for example
                // black, white, contrast white. Here we choose first, but you might do differently
                string finalLogo = null;
                // serach for all files that match file name in Logo element but with any suffix (like "Logo.black.png, Logo.white.png etc)
                foreach (var logoFile in Directory.GetFiles(System.IO.Path.Combine(dir, Path.GetDirectoryName(pathToLogo)),
                    System.IO.Path.GetFileNameWithoutExtension(pathToLogo) + "*" + Path.GetExtension(pathToLogo)))
                {
                    finalLogo = logoFile;
                    break;
                }

                if (File.Exists(finalLogo))
                {
                    return (Bitmap)Image.FromFile(finalLogo);
                }
            }
            
            return null;
        }

        /// <summary>
        /// Gets the logo of a Windows App (WinRT, UWP...).
        /// </summary>
        /// <param name="appFullDirectoryPath">The installation directory of the app</param>
        /// <param name="manifestData">The manifest info, as obtained by <see cref="ApplicationUtils.GetWindowsAppManifestData()"/></param>
        /// <returns>The application icon or null if an icon is not found.</returns>
        public Bitmap GetWindowsAppIcon(string appFullDirectoryPath, WindowsAppManifestData manifestData)
        {
            string finalLogo = null;

            // serach for all files that match file name in Logo element but with any suffix (like "Logo.black.png, Logo.white.png etc)
            string fileSearchPattern = Path.GetFileNameWithoutExtension(manifestData.PathToLogo) + "*" + Path.GetExtension(manifestData.PathToLogo);

            foreach (var logoFile in Directory.GetFiles(Path.Combine(appFullDirectoryPath, Path.GetDirectoryName(manifestData.PathToLogo)), fileSearchPattern))
            {
                finalLogo = logoFile;
                break;
            }

            if (File.Exists(finalLogo))
            {
                return (Bitmap)Image.FromFile(finalLogo);
            }

            return null;
        }

        public WindowsAppManifestData GetWindowsAppManifestData(string appFullDirectoryPath)
        {
            WindowsAppManifestData manifestInfo = null;
            
            var manifestPath = Path.Combine(appFullDirectoryPath, "AppxManifest.xml");

            if (File.Exists(manifestPath))
            {
                manifestInfo = new WindowsAppManifestData();
                
                using (var fs = File.OpenRead(manifestPath))
                {
                    var manifest = XDocument.Load(fs);
                    const string ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

                    manifestInfo.Version = manifest.Root
                        .Element(XName.Get("Identity", ns))
                        ?.Attribute(XName.Get("Version", ns)).Value;

                    var properties = manifest.Root.Element(XName.Get("Properties", ns));
                    if (properties != null)
                    {
                        manifestInfo.PathToLogo = properties.Element(XName.Get("Logo", ns))?.Value;
                        manifestInfo.PublisherDisplayName = properties.Element(XName.Get("PublisherDisplayName", ns))?.Value;
                    }
                }
            }

            return manifestInfo;
        }

        public string[] GetInstalledWindowsAppPackageNames()
        {
            using (RegistryKey hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32))
            {
                RegistryKey packagesKey = hkcr.OpenSubKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
                return  packagesKey?.GetSubKeyNames();
            }
        }

        public static WindowsAppPackageData GetWindowsAppResourceData(string packageName)
        {
            WindowsAppPackageData packageData = null;

            // https://stackoverflow.com/questions/23331385/how-to-obtain-the-display-name-of-installed-metro-apps
            using (RegistryKey hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32))
            using (RegistryKey packagesKey = hkcr.OpenSubKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages"))
            using (RegistryKey packageKey = packagesKey.OpenSubKey(packageName))
            {
                if (packageKey != null && packageKey.SubKeyCount > 0)
                {
                    string[] packageAppNames = packageKey.GetSubKeyNames();

                    foreach (string packageAppName in packageAppNames)
                    {
                        using (RegistryKey packageAppKey = packageKey.OpenSubKey(packageAppName))
                        {
                            if (packageAppKey != null && packageAppKey.SubKeyCount > 0)
                            {
                                packageData = GetAppPackageInfoFromRegistry(packageKey, packageAppKey);

                                if (packageData.Name.StartsWith("@"))
                                    packageData.Name = GetResourceString(packageData.Name);

                                if (packageData.Description.StartsWith("@"))
                                    packageData.Description = GetResourceString(packageData.Description);
                            }
                        }
                    }
                }
            }

            return packageData;
        }

        private static WindowsAppPackageData GetAppPackageInfoFromRegistry(RegistryKey packageKey, RegistryKey packageAppKey)
        {
            WindowsAppPackageData packageData = null;

            using (RegistryKey capabilitiesKey = packageAppKey.OpenSubKey("Capabilities"))
            {

                if (capabilitiesKey != null)
                {
                    packageData = new WindowsAppPackageData();
                    packageData.Name = (string)capabilitiesKey.GetValue("ApplicationName");
                    packageData.Description = (string)capabilitiesKey.GetValue("ApplicationDescription");
                    packageData.PackageId = (string)packageKey.GetValue("PackageID");
                    packageData.RootFolder = (string)packageKey.GetValue("PackageRootFolder");
                }
            }

            return packageData;
        }

        private static string GetResourceString(string resString)
        {
            StringBuilder sb = new StringBuilder(1024);

            if (ShlwApi.SHLoadIndirectString(resString, sb, (uint)sb.Capacity, IntPtr.Zero) == 0)
                return sb.ToString();

            return null;
        }

        public class WindowsAppManifestData
        {
            public string AppDirectory { get; internal set; }

            public string Version { get; internal set; }

            public string PublisherDisplayName { get; internal set; }

            public string PathToLogo { get; internal set; }
        }

        public class WindowsAppPackageData
        {
            public string Name { get; internal set; }

            public string Description { get; internal set; }

            public string PackageId { get; internal set; }

            public string RootFolder { get; internal set; }
        }
    }
}
