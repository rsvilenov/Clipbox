using System;
using System.Collections.Generic;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class DiagnosticsReport
    {
        public FailureInfo FailureInfo { get; set; }

        public SystemInfo SystemInfo { get; set; }
        
        public List<ExtensionInfo> LoadedExtensions { get; set; }
    }
}
