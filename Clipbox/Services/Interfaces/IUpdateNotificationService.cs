using System;

namespace Clipbox.Services.Interfaces
{
    interface IUpdateNotificationService
    {
        bool IsUpdateAvailable { get; }

        event EventHandler UpdateAvailable;
    }
}
