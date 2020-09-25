using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardItemSource
    {
        IWindowInfo SourceWindowInfo { get; }

        IMachineInfo SourceMachineInfo { get; }

        ISessionInfo SourceSessionInfo { get; }
    }
}
