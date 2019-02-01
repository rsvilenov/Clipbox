using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class SourceWindowInfo : IWindowInfo
    {
        public string ApplicationPath { get; internal set; }

        public string Caption { get; internal set; }

        public byte[] Icon { get; internal set; }
    }
}
