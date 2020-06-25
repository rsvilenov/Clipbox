using System;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class SystemInfo
    {
        public int CpuCount { get; set; }
        
        public string CpuName { get; set; }

        public bool Is64BitOs { get; set; }

        public double OsVersion { get; set; }

        public long AvailableFreeSpace { get; set; }

        public long WorkingSet { get; set; }

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
