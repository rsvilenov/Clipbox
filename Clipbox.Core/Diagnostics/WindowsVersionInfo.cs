using System;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class WindowsVersionInfo
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
        public bool Is64Bit { get; set; }
    }
}
