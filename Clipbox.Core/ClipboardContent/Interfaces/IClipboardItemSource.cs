using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardItemSource
    {
        IWindowInfo SourceWindowInfo { get; }

        string MachineName { get; }

        MachineType MachineType { get; }

        string MachineUniqueId { get; }

        string UserName { get; }
    }
}
