using System.Windows;

namespace Clipbox.Services.Interfaces
{
    public interface IPopupHintService
    {
        void ShowHintPopup(string popupName, FrameworkElement anchorElement, bool isTopPointing);
    }
}
