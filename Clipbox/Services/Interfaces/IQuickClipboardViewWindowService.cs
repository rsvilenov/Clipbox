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
        
        IReadOnlyList<string> GetSelectedItemIds();

        IReadOnlyDictionary<string, IReadOnlyCollection<int>> GetSelectedMultiItemsIds();
    }
}
