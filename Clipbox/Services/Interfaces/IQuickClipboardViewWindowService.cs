using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface IQuickClipboardViewWindowService : IClipboardViewWindowServiceBase
    {
        void ShowQuickClipboardViewWindow();

        void HideQuickClipboardViewWindow();

        void FocusNextItem();

        bool IsWindowVisible { get; }

        void LoadClipboardItem(string clipboardItemId);

        void StoreState();

        IReadOnlyCollection<string> SelectedItemIds { get; }

        IReadOnlyDictionary<string, IReadOnlyCollection<int>> SelectedMultiItemIds { get; }
    }
}
