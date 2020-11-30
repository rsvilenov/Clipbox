using Clipbox.Core.Diagnostics;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clipbox.Infrastructure.Diagnostics
{
    public static class DiagnosticsReportGenerator
    {
        public static DiagnosticsReport Generate(Exception ex, bool includeExtensionInfo = false)
        {
            return GetReport(new FailureInfo(ex), includeExtensionInfo);
        }

        public static DiagnosticsReport Generate(string failureMessage, bool includeExtensionInfo = false)
        {
            return GetReport(new FailureInfo(failureMessage), includeExtensionInfo);
        }

        private static DiagnosticsReport GetReport(FailureInfo fi, bool includeExtensionInfo)
        {
            var report = new DiagnosticsReport
            {
                FailureInfo = fi,
                SystemState = GetSystemInfo()
            };

            if (includeExtensionInfo)
            {
                try
                {
                    report.LoadedExtensions = GetLoadedExtensions()?.ToList();
                }
                catch { }
            }

            return report;
        }

        private static IEnumerable<ExtensionInfo> GetLoadedExtensions()
        {
            IEnumerable<Type> extensionTypes =
                AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IClipboxExtension).IsAssignableFrom(p) 
                    && !p.IsInterface);

            foreach (Type extType in extensionTypes)
            {
                IExtensionInfo extInfo = null;

                if (ClipboxExtensionAttribute.IsDefinedFor(extType))
                {
                    extInfo = ClipboxExtensionAttribute.GetExtensionInfo(extType);

                    yield return new ExtensionInfo
                    {
                        ExtensionAuthor = extInfo.Author,
                        ExtensionDescription = extInfo.Description,
                        ExtensionType = extType.FullName,
                        ExtensionModuleName = extType.Module.FullyQualifiedName,
                        ExtensionVersion = extInfo.Version?.ToString()
                    };
                }
            }
        }

        private static SystemState GetSystemInfo()
        {
            SystemState si = new SystemState
            {
                WorkingSet = Environment.WorkingSet
            };

            try
            {
                PerformanceUtils.SystemPerformanceInformation pInfo = PerformanceUtils.GetPerformanceInformation();
                if (pInfo != null)
                {
                    si.CommitTotal = pInfo.CommitTotal;
                    si.CommitLimit = pInfo.CommitLimit;
                    si.CommitPeak = pInfo.CommitPeak;
                    si.PhysicalAvailable = pInfo.PhysicalAvailable;
                    si.PhysicalTotal = pInfo.PhysicalTotal;
                    si.KernelTotal = pInfo.KernelTotal;
                    si.KernelPaged = pInfo.KernelPaged;
                    si.KernelNonPaged = pInfo.KernelNonPaged;
                    si.SystemCache = pInfo.SystemCache;
                    si.ThreadCount = pInfo.ThreadCount;
                    si.ProcessCount = pInfo.ProcessCount;
                    si.HandlesCount = pInfo.HandlesCount;
                }
            }
            catch { }

            try
            {
                string appDataRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                DriveInfo appDataDrive = DriveInfo.GetDrives()
                    .FirstOrDefault(di => di.RootDirectory.FullName == appDataRoot);
                si.AvailableFreeSpace = appDataDrive?.AvailableFreeSpace ?? 0;
            }
            catch { }

            return si;
        }
    }
}
