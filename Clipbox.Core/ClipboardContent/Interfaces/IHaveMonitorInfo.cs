namespace Clipbox.Core.ClipboardContent.Interfaces
{
    public interface IHaveMonitorInfo
    {
        int WorkAreaWidth { get; }
        int WorkAreaHeight { get; }
        double DPI { get; }
    }
}
