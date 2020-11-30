using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Win32;
using System;

namespace Clipbox.Infrastructure.Diagnostics
{
    public static class AggregateReportGenerator
    {
        /// <summary>
        /// Creates a new report, filled only with information about the product version and the system,
        /// but with no error data
        /// </summary>
        /// <returns></returns>
        public static AggregateReport CreateNewEmptyReport(Version productVersion)
        {

            OsVersionUtils.WindowsVersion osVersion;
            try { osVersion = OsVersionUtils.GetWindowsVersion(); }
            catch { osVersion = new OsVersionUtils.WindowsVersion(); }

            string cpuName = null;
            try
            {
                cpuName = CpuUtils.GetCpuName();
            }
            catch { }

            SystemInfo systemInfo = new SystemInfo
            {
                CpuCount = Environment.ProcessorCount,
                CpuName = cpuName,
                WindowsVersionInfo = new WindowsVersionInfo
                {
                    BuildNumber = osVersion.BuildNumber,
                    EditionId = osVersion.EditionId,
                    LegacyVersionString = osVersion.LegacyVersionString,
                    MajorVersion = osVersion.MajorVersion,
                    MinorVersion = osVersion.MinorVersion,
                    UBR = osVersion.UBR,
                    Is64Bit = Environment.Is64BitOperatingSystem
                }
            };

            return new AggregateReport(productVersion, systemInfo, Environment.Is64BitProcess);
        }
    }
}
