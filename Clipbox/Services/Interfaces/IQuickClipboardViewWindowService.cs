using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Clipbox.Services.Interfaces
{
    public interface IQuickClipboardViewWindowService : IClipboardViewWindowServiceBase
    {
        void ShowQuickClipboardViewWindow();

        void HideQuickClipboardViewWindow();

        void FocusNextItem();
        
        void SaveChangedSettings();

        bool IsWindowVisible { get; }
        
        IReadOnlyCollection<string> SelectedItemIds { get; }

        IReadOnlyDictionary<string, IReadOnlyCollection<int>> SelectedMultiItemIds { get; }
    }
}
