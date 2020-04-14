using Clipbox.Core.ClipboardContent.Enums;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IMachineInfo
    {
        string MachineName { get; }

        string MachineUniqueId { get; }

        MachineType MachineType { get; }
    }
}
