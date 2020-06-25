using Clipbox.Win32.Native;
using System.Runtime.InteropServices;

namespace Clipbox.Win32
{
    public static class PerformanceUtils
    {
        public static SystemPerformanceInformation GetPerformanceInformation()
        {
            NativeStructures.PerformanceInformation pi = 
                new NativeStructures.PerformanceInformation();
            if (PsApi.GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                return new SystemPerformanceInformation
                {
                    CommitLimit = pi.CommitLimit.ToInt64() * pi.PageSize.ToInt64(),
                    CommitPeak = pi.CommitPeak.ToInt64() * pi.PageSize.ToInt64(),
                    CommitTotal = pi.CommitTotal.ToInt64() * pi.PageSize.ToInt64(),
                    PhysicalAvailable = pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64(),
                    PhysicalTotal = pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64(),
                    SystemCache = pi.SystemCache.ToInt64() * pi.PageSize.ToInt64(),
                    KernelTotal = pi.KernelTotal.ToInt64() * pi.PageSize.ToInt64(),
                    KernelPaged = pi.KernelPaged.ToInt64() * pi.PageSize.ToInt64(),
                    KernelNonPaged = pi.KernelNonPaged.ToInt64() * pi.PageSize.ToInt64(),
                    HandlesCount = pi.HandlesCount,
                    ProcessCount = pi.ProcessCount,
                    ThreadCount = pi.ThreadCount
                };
            }
            else
            {
                return null;
            }
        }
        

        public class SystemPerformanceInformation
        {
            public long CommitTotal { get; set; }
            public long CommitLimit { get; set; }
            public long CommitPeak { get; set; }
            public long PhysicalTotal { get; set; }
            public long PhysicalAvailable { get; set; }
            public long SystemCache { get; set; }
            public long KernelTotal { get; set; }
            public long KernelPaged { get; set; }
            public long KernelNonPaged { get; set; }
            public int HandlesCount { get; set; }
            public int ProcessCount { get; set; }
            public int ThreadCount { get; set; }
        }
    }
}
