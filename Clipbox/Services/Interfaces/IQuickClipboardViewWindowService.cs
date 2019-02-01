using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Clipbox.Services.Interfaces
{
    public interface IQuickClipboardViewWindowService
    {
        void ShowQuickClipboardViewWindow();

        void HideQuickClipboardViewWindow();

        void FocusNextItem();

        IEnumerable<IFlowParameter> GetParameters();

        void SaveChangedSettings();

        bool IsWindowVisible { get; }
        
        IReadOnlyList<string> SelectedItemIds { get; }

        IReadOnlyDictionary<string, ReadOnlyCollection<int>> SelectedMultiItemsIds { get; }
        
        event EventHandler<SelectionEventArgs> SelectionChanged;
    }
}
