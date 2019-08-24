using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Clipbox.Views.Interfaces
{
    public interface IMainWindow
    {
        void Show();
        void Close();
        bool IsLoaded { get; }
        bool IsVisible { get; }

        IReadOnlyList<IFlowParameter> GetFlowParameters();

        event RoutedEventHandler Loaded;

        event EventHandler<SelectionEventArgs> ClipboardItemSelectionChanged;

        event EventHandler<CombinabilityEventArgs> ClipbardItemsCombinabilityCheckRequested;

        event EventHandler<SelectionEventArgs> SetItemsToClipboardRequested;
    }
}
