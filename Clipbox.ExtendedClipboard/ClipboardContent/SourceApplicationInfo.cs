using Clipbox.Core.Extensibility.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class SourceApplicationInfo : IApplicationInfo
    {
        public string ApplicationPath { get; internal set; }

        public string ProductName { get; internal set; }

        public string ProductVersion { get; internal set; }

        public string FileDescription { get; internal set; }

        public string CompanyName { get; internal set; }
    }
}
