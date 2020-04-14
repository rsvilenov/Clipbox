using Clipbox.Core.ClipboardContent.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.ExtendedClipboard.ClipboardContent
{
    public class SourceDeviceInfo
    {
        public string MachineName { get; internal set; }

        public string MachineUniqueId { get; internal set; }

        public MachineType MachineType { get; internal set; }
    }
}
