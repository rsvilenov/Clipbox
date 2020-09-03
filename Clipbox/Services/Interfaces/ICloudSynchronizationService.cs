using Clipbox.Services.EventArgsClasses;
using System;
using System.IO;

namespace Clipbox.Services.Interfaces
{
    public interface ICloudSynchronizationService
    {
        bool IsNetworkAvailable { get; }

        bool IsAuthenticated { get; }

        bool IsEnabled { get; set; }

        bool IsInErrorState { get; }

        string GetLastError();

        event EventHandler NetworkAvailabilityChanged;

        event EventHandler AuthenticationStateChanged;

        event EventHandler<ErrorEventArgs> ErrorOccurred;

        event EventHandler<SyncEventArgs> ItemSynced;
    }
}
