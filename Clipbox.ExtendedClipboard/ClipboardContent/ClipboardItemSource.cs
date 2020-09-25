using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class ClipboardItemSource : IClipboardItemSource
    {
        public IWindowInfo SourceWindowInfo { get; internal set; }

        public IMachineInfo SourceMachineInfo { get; internal set; }

        public ISessionInfo SourceSessionInfo { get; internal set; }

    }
}
