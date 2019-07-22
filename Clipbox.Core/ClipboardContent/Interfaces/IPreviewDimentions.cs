namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IPreviewDimentions
    {
        // monitor info
        int MonitorWidth { get; }
        int MonitorHeight { get; }
        int MonitorDPI { get; }

        // item preview info
        int PreviewWidth { get; }
        int PreviewHeight { get; }
    }
}
