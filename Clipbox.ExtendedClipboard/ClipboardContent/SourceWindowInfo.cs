using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class SourceWindowInfo : IWindowInfo
    {
        public string Caption { get; internal set; }

        public byte[] Icon { get; internal set; }

        public IApplicationInfo ApplicationInfo { get; internal set; }
    }
}
