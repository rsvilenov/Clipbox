using System;

namespace Clipbox.Services.Interfaces
{
    public interface IUpdateNotificationService
    {
        bool IsUpdateAvailable { get; }

        event EventHandler UpdateAvailable;

        void LaunchUpdate();
    }
}
