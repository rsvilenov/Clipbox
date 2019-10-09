using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.ClipboardContent.Interfaces;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    internal class ClipboardItemSource : IClipboardItemSource
    {
        public IWindowInfo SourceWindowInfo { get; internal set; }

        public string MachineName { get; internal set; }

        public string MachineUniqueId { get; internal set; }

        public string UserName { get; internal set; }
    }
}
