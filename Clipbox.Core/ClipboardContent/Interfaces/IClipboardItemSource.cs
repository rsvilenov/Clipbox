using Clipbox.Core.Extensibility.Contracts;

namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IClipboardItemSource
    {
        IWindowInfo SourceWindowInfo { get; }

        string MachineName { get; }

        string UserName { get; }
    }
}
