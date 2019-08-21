namespace Clipbox.Services.Interfaces
{
    public interface IMainClipboardViewWindowService : IClipboardViewWindowServiceBase
    {
        void OpenOnMonitor(Win32.MonitorUtils.DisplayMonitor dm);

        void Close();

        bool IsOpen { get; }
    }
}
