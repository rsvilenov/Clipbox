using Clipbox.Services.Enums;
using System;

namespace Clipbox.Services.Interfaces
{
    public interface ITrayIconService
    {
        void ShowTrayIcon();

        void HideTrayIcon();

        void ShowToolTip(string toolTipText);

        TrayIconVisibilityPreference GetVisibilityPreference();

        void SetVisibilityPreference(TrayIconVisibilityPreference preference);
        
        event EventHandler TrayIconClicked;
    }
}
