using System;
using System.IO;

namespace Clipbox.Services.Interfaces
{
    internal interface ICloudSynchronizationService
    {
        bool IsNetworkAvailable { get; }

        bool IsAuthenticated { get; }

        bool IsEnabled { get; set; }

        event EventHandler NetworkAvailabilityChanged;

        event EventHandler<ErrorEventArgs> ErrorOccurred;
    }
}
