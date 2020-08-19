using Clipbox.Win32.Native;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Clipbox.Win32
{
    public class ApplicationUtils
    {
        public static List<InstalledAppInfo> GetAllInstalledApplications()
        {
            List<InstalledAppInfo> installedApps = new List<InstalledAppInfo>();

            installedApps.AddRange(CollectFromWindowsApps());

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
                // distinct the apps
                .GroupBy(ia => ia.InstallLocation)
                .Select(g => g.First())
                // add the system accessories (they are all in %windir%\system32
                // so we are adding them here, after the distinct by folder)
                .Union(CollectFromWindowsAccessories())
                .ToList();
        }

        private static IEnumerable<InstalledAppInfo> CollectFromWindowsAccessories()
        {
            string[] accessories = new string[]
            {
                "notepad.exe",
                "mspaint.exe"
            };
            
            foreach (string tool in accessories)
            {
                string accessoryFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), tool);
                if (File.Exists(accessoryFullPath))
                {
                    yield return GetAppInfoFromPath(accessoryFullPath);
                }
            }
        }

        private static IEnumerable<InstalledAppInfo> CollectFromWindowsApps()
        {
            if (_windowsAppInfoCache == null)
            {
                CacheWindowsAppInfo();
            }

            foreach (var appInfo in _windowsAppInfoCache)
            {
                string appInstallLocation = string.IsNullOrEmpty(appInfo.ManifestData.AppFolderFullPath)
                    ? appInfo.PackageData.RootFolder
                    : appInfo.ManifestData.AppFolderFullPath;

                yield return new InstalledAppInfo
                {
                    DisplayIcon = appInfo.Icon,
                    DisplayName = appInfo.PackageData.Name,
                    InstallLocation = appInstallLocation
                };
            }

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

            string packagesRoot = GetWindowsAppPackagesRootFolder();
            if (packagesRoot != null &&
                exePath.StartsWith(packagesRoot) &&
                fileVersionInfo.FileDescription != fileVersionInfo.ProductName)
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
                                        using (Icon icon = new Icon(installedAppInfo.DisplayIconPath))
                                        {
                                            installedAppInfo.DisplayIcon = icon.ToBitmap();
                                        }
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

        public static Bitmap ExtractIconFromModule(string path, int iconIndex = 0)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"{path} does not exist.");
            }

            Bitmap iconBmp = null;
            
            IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
            IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };

            int readIconCount = Shell32.ExtractIconEx(path, iconIndex, hIconEx, hDummy, 1);

            if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
            {
                using (Icon icon = (Icon)Icon.FromHandle(hIconEx[0]))
                {
                    iconBmp = icon.ToBitmap();
                }
            }

            return iconBmp;
        }

        /// <summary>
        /// Retrieves the icon of a WinRT/UWP applications.
        /// </summary>
        /// <param name="exeFullPath">The fill path of the executable.</param>
        /// <returns>The icon or null</returns>
        public static Bitmap GetWindowsAppIcon(string exeFullPath)
        {
            // https://stackoverflow.com/questions/32122679/getting-icon-of-modern-windows-app-from-a-desktop-application

            WindowsAppInfo appInfo = GetWinwodsAppInfo(exeFullPath);
            return appInfo?.Icon?.Clone() as Bitmap;
        }

        /// <summary>
        /// Gets the logo of a Windows App (WinRT, UWP...).
        /// </summary>
        /// <param name="appFullDirectoryPath">The installation directory of the app</param>
        /// <param name="manifestData">The manifest info, as obtained by <see cref="ApplicationUtils.GetWindowsAppManifestData()"/></param>
        /// <returns>The application icon or null if an icon is not found.</returns>
        private static Bitmap GetWindowsAppIcon(string appFullDirectoryPath, WindowsAppManifestData manifestData)
        {
            string finalLogo = null;

            // serach for all files that match file name in Logo element but with any suffix (like "Logo.black.png, Logo.white.png etc)
            string fileSearchPattern = Path.GetFileNameWithoutExtension(manifestData.PathToLogo) + "*" + Path.GetExtension(manifestData.PathToLogo);
            string logoFolder = Path.Combine(appFullDirectoryPath, Path.GetDirectoryName(manifestData.PathToLogo));
            if (!Directory.Exists(logoFolder))
            {
                return null;
            }

            foreach (var logoFile in Directory.GetFiles(logoFolder, fileSearchPattern))
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

        private static IEnumerable<WindowsAppManifestData> GetWindowsAppManifestData(WindowsAppPackageData pkgData)
        {
            if (pkgData == null 
                || string.IsNullOrEmpty(pkgData.PackageId) 
                || string.IsNullOrEmpty(pkgData.RootFolder))
            {
                return Enumerable.Empty<WindowsAppManifestData>();
            }

            List<WindowsAppManifestData> manifestInfo = new List<WindowsAppManifestData>();

            string appFullDirectoryPath = pkgData.RootFolder;
            string packageId = pkgData.PackageId;

            var manifestPath = Path.Combine(appFullDirectoryPath, "AppxManifest.xml");

            if (File.Exists(manifestPath))
            {
                using (var fs = File.OpenRead(manifestPath))
                {
                    var manifest = XDocument.Load(fs);

                    string ns = manifest.Root.GetDefaultNamespace().NamespaceName; //"http://schemas.microsoft.com/appx/manifest/foundation/windows10";

                    const string uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
                    const string m2 = "http://schemas.microsoft.com/appx/2013/manifest";

                    string appNs = ns.EndsWith("windows10") ? uap : m2;


                    string packageVersion = manifest.Root.Element(XName.Get("Identity", ns))
                        .Attribute("Version")?.Value;

                    string pathToPackageLogo = null;
                    string publisherDisplayName = null;
                    string packageDisplayName = null;
                    var packageProperties = manifest.Root.Element(XName.Get("Properties", ns));
                    if (packageProperties != null)
                    {
                        pathToPackageLogo = packageProperties.Element(XName.Get("Logo", ns))?.Value;
                        publisherDisplayName = packageProperties.Element(XName.Get("PublisherDisplayName", ns))?.Value;

                        packageDisplayName = packageProperties.Element(XName.Get("DisplayName", ns))?.Value;
                        if (packageDisplayName != null && packageDisplayName.StartsWith("ms-resource:"))
                        {
                            string resourceKeyString = $"@{{{packageId}}}?{packageDisplayName}";
                            packageDisplayName = GetResourceString(resourceKeyString);
                        }
                    }

                    var applications = manifest.Root.Element(XName.Get("Applications", ns));
                    foreach (var application in applications.Elements(XName.Get("Application", ns)))
                    {
                        string appDirectory = appFullDirectoryPath;
                        string executable = application.Attribute("Executable")?.Value;
                        if (!string.IsNullOrEmpty(executable))
                        {
                            string subDir = Path.GetDirectoryName(executable);
                            if (!string.IsNullOrEmpty(subDir))
                            {
                                appDirectory = Path.Combine(appDirectory, subDir);
                            }
                        }
                        
                        var visualElements = application.Element(XName.Get("VisualElements", appNs));
                        string pathToLogo = null;
                        string displayName = null;
                        string description = null;
                        if (visualElements != null)
                        {
                            displayName = visualElements.Attribute("DisplayName")?.Value;
                            if (displayName != null && displayName.StartsWith("ms-resource:"))
                            {
                                string resourceKeyString = $"@{{{packageId}}}?{displayName}";
                                displayName = GetResourceString(resourceKeyString);
                            }

                            description = visualElements.Attribute("Description")?.Value;
                            pathToLogo = visualElements.Attributes()
                                .FirstOrDefault(a => a.Name.LocalName.EndsWith("Logo", StringComparison.OrdinalIgnoreCase))
                                ?.Value;
                            manifestInfo.Add(new WindowsAppManifestData
                            {
                                AppFolderFullPath = appDirectory,
                                PathToLogo = pathToLogo ?? pathToPackageLogo,
                                PublisherDisplayName = publisherDisplayName,
                                Version = packageVersion,
                                Description = description,
                                ExecutableName = executable,
                                DisplayName = displayName ?? packageDisplayName
                            });
                        }
                    }

                    if (!manifestInfo.Any())
                    {
                        manifestInfo.Add(new WindowsAppManifestData
                        {
                            AppFolderFullPath = appFullDirectoryPath,
                            PathToLogo = pathToPackageLogo,
                            PublisherDisplayName = publisherDisplayName,
                            Version = packageVersion,
                            DisplayName = packageDisplayName
                        });
                    }
                }
            }

            return manifestInfo;
        }

        
        private static ConcurrentBag<WindowsAppInfo> _windowsAppInfoCache;
        /// <summary>
        /// Caches the information for all WinRT/UWP apllications,
        /// installed on the system, so that the future access to
        /// this information will be much faster.
        /// </summary>
        public static void CacheWindowsAppInfo()
        {
            var newBag = new ConcurrentBag<WindowsAppInfo>();
            Interlocked.Exchange(ref _windowsAppInfoCache, newBag); ;

            string[] packageNames = GetInstalledWindowsAppPackageNames();
            if (packageNames != null)
            {
                foreach (var packageName in packageNames)
                {
                    foreach (var appInfo in GetWindowsAppsInfoForPackage(packageName))
                    {
                        if (appInfo != null 
                            && appInfo.PackageData != null 
                            && appInfo.ManifestData != null)
                        {
                            _windowsAppInfoCache.Add(appInfo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if an applcation is an WinRT/UWP application.
        /// </summary>
        /// <param name="exeFullPath">The full path of the program.</param>
        /// <returns>True if the app is WinRT/UWP</returns>
        public static bool CheckIsWindowsApp(string exeFullPath)
        {
            string packagesRoot = GetWindowsAppPackagesRootFolder();
            return packagesRoot != null && exeFullPath.StartsWith(packagesRoot);
        }

        private static IEnumerable<WindowsAppInfo> GetWindowsAppsInfoForPackage(string packageName)
        {
            var packageData = GetWindowsAppPackageData(packageName);
            if (packageData == null
                || !Directory.Exists(packageData.RootFolder))
            {
                yield break;
            }

            foreach (var manifestData in GetWindowsAppManifestData(packageData))
            {

                yield return new WindowsAppInfo
                {
                    ManifestData = manifestData,
                    PackageData = packageData,
                    Icon = GetWindowsAppIcon(packageData.RootFolder, manifestData)
                };
            }
        }

        private static string _packagesRootFolder;
        private static string GetWindowsAppPackagesRootFolder()
        {
            if (_packagesRootFolder == null)
            {
                if (IntPtr.Size == 4 && Environment.Is64BitOperatingSystem)
                {
                    throw new PlatformNotSupportedException("Please compile this program for 64 bit.");
                }

                using (var appx = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Appx"))
                {
                    _packagesRootFolder = appx.GetValue("PackageRoot") as string;
                }
            }

            return _packagesRootFolder;
        }

        /// <summary>
        /// Gets information about a WinRT/UWP application.
        /// </summary>
        /// <param name="exeFullPath">The full path of the exe file.</param>
        /// <returns>True if the application is WinRT/UWP</returns>
        public static WindowsAppInfo GetWinwodsAppInfo(string exeFullPath)
        {
            if (!CheckIsWindowsApp(exeFullPath))
            {
                throw new ArgumentException($"Not a windows app: {exeFullPath}");
            }
            string exeDir = Path.GetDirectoryName(exeFullPath);

            WindowsAppInfo appInfo = _windowsAppInfoCache
                .FirstOrDefault(ai => 
                ai.ManifestData.AppFolderFullPath.Equals(exeDir, StringComparison.OrdinalIgnoreCase)
                || ai.PackageData.RootFolder.Equals(exeDir, StringComparison.OrdinalIgnoreCase));

            if (appInfo == null)
            {
                // newly installed? reload the cache
                CacheWindowsAppInfo();

                appInfo = _windowsAppInfoCache
                    .FirstOrDefault(ai =>
                    ai.ManifestData.AppFolderFullPath.Equals(exeDir, StringComparison.OrdinalIgnoreCase)
                    || ai.PackageData.RootFolder.Equals(exeDir, StringComparison.OrdinalIgnoreCase));

            }

            return appInfo;
        }

        private static string[] GetInstalledWindowsAppPackageNames()
        {
            using (RegistryKey hkcr = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32))
            {
                RegistryKey packagesKey = hkcr.OpenSubKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
                return packagesKey?.GetSubKeyNames();
            }
        }

        private static WindowsAppPackageData GetWindowsAppPackageData(string packageName)
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

                                if (packageData != null)
                                {
                                    if (packageData.Name.StartsWith("@"))
                                        packageData.Name = GetResourceString(packageData.Name);

                                    if (packageData.Description.StartsWith("@"))
                                        packageData.Description = GetResourceString(packageData.Description);
                                }
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
            {
                return sb.ToString();
            }

            return null;
        }

        public class WindowsAppManifestData
        {
            public string AppFolderFullPath { get; internal set; }

            public string Version { get; internal set; }

            public string PublisherDisplayName { get; internal set; }

            public string PathToLogo { get; internal set; }

            public string Description { get; internal set; }

            public string ExecutableName { get; internal set; }

            public string DisplayName { get; set; }
        }

        public class WindowsAppPackageData
        {
            public string Name { get; internal set; }

            public string Description { get; internal set; }

            public string PackageId { get; internal set; }

            public string RootFolder { get; internal set; }
        }

        public class WindowsAppInfo
        {
            public WindowsAppPackageData PackageData { get; internal set; }

            public WindowsAppManifestData ManifestData { get; internal set; }


            private Bitmap _icon;
            public Bitmap Icon
            {
                // this object stays in the cache, so if someone disposes it
                // mess will insue... so don't give users the direct reference...
                get { return _icon?.Clone() as Bitmap; }
                set { _icon = value; }
            }
        }

        public class InstalledAppInfo
        {
            public string InstallLocation { get; internal set; }

            public string DisplayName { get; internal set; }

            public string DisplayIconPath { get; internal set; }

            public Bitmap DisplayIcon { get; internal set; }
        }

    }
}
