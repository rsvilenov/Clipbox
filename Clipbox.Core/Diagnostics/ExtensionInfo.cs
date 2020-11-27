using System;

namespace Clipbox.Core.Diagnostics
{
    [Serializable]
    public class ExtensionInfo
    {
        public string ExtensionType { get; set; }

        public string ExtensionVersion { get; set; }

        public string ExtensionAuthor { get; set; }

        public string ExtensionDescription { get; set; }

        public string ExtensionModuleName { get; set; }
    }
}
