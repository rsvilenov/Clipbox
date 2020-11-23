using System.Windows;

namespace Clipbox.Services.Interfaces
{
    public interface IPopupHintService
    {
        bool HasPopupBeenSeen(string popupName);
        bool TryShowHintPopup(string popupName, FrameworkElement anchorElement, bool isTopPointing);
        void HideHintPopup(bool setAsSeen = false);
    }
}
