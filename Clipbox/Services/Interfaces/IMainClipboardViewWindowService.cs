using Clipbox.Services.EventArgsClasses;
using System;
using System.ComponentModel;

namespace Clipbox.Services.Interfaces
{
    public interface IMainClipboardViewWindowService : IClipboardViewWindowServiceBase
    {
        void OpenOnMonitor(Win32.MonitorUtils.DisplayMonitor dm);

        void Close();

        bool IsOpen { get; }

        event EventHandler<SetToClipboardEventArgs> SetItemsToClipboardRequested;

        event EventHandler SettingsWindowOpenRequested;

        event EventHandler RebuildIndexRequested;

        event EventHandler<CancelEventArgs> OpenMainWindowRequested;
    }
}
