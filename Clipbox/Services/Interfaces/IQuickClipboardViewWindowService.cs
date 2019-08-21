namespace Clipbox.Services.Interfaces
{
    public interface IQuickClipboardViewWindowService : IClipboardViewWindowServiceBase
    {
        void ShowQuickClipboardViewWindow();

        void HideQuickClipboardViewWindow();

        void FocusNextItem();
        
        void SaveChangedSettings();

        bool IsWindowVisible { get; }
    }
}
