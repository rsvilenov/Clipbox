using Clipbox.Services.EventArgsClasses;
using System;
using System.Windows;

namespace Clipbox.Views.Interfaces
{
    public interface IMainWindow
    {
        void Show();
        void Close();
        bool IsLoaded { get; }
        bool IsVisible { get; }
        event RoutedEventHandler Loaded;

        event EventHandler<SelectionEventArgs> ClipboardItemSelectionChanged;
    }
}
