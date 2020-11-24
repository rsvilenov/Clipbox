using Clipbox.Services.Enums;
using System.Windows;

namespace Clipbox.Services.Interfaces
{
    public interface IPopupHintService
    {
        bool HasPopupBeenSeen(string popupName);
        bool TryShowHintPopup(string popupName, FrameworkElement anchorElement, PopupHintArrowDirection arrowDirection);
        void HideHintPopup(bool setAsSeen = false);
    }
}
