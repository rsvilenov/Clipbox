namespace Clipbox.Services.Interfaces
{
    public interface ISettingsWindowService
    {
        void OpenSettingsWindow();

        bool IsSettingsWindowOpen { get; }
    }
}
