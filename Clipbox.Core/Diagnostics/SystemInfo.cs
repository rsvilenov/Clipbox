using System;
using System.Collections.Generic;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class SystemInfo
    {
        public WindowsVersionInfo WindowsVersionInfo { get; set; }
        public int CpuCount { get; set; }
        public string CpuName { get; set; }
        public List<DisplayInfo> Displays { get; set; }
    }
}
