using Clipbox.Services.Enums;
using System;

namespace Clipbox.Services.Interfaces
{
    public interface ITrayIconService
    {
        void ShowTrayIcon();

        void HideTrayIcon();

        TrayIconVisibilityPreference GetVisibilityPreference();

        void SetVisibilityPreference(TrayIconVisibilityPreference preference);
        
        event EventHandler TrayIconClicked;
    }
}
