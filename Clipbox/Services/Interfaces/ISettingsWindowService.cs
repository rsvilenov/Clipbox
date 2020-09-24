using Clipbox.Core.Extensibility.Enums;

namespace Clipbox.Services.Interfaces
{
    public interface ISettingsWindowService
    {
        void OpenSettingsWindow(SettingsCategory highlightCategory);

        bool IsSettingsWindowOpen { get; }
    }
}
