using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;
using System;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    [Serializable]
    public class SourceMachineInfo : IMachineInfo
    {
        public string MachineName { get; internal set; }

        public string MachineUniqueId { get; internal set; }

        public MachineType MachineType { get; internal set; }
    }
}
