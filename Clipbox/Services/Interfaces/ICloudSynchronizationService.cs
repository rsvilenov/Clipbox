using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.IO;

namespace Clipbox.Services.Interfaces
{
    public interface ICloudSynchronizationService
    {
        bool IsNetworkAvailable { get; }

        bool IsAuthenticated { get; }

        bool IsAuthenticating { get; }

        bool IsEnabled { get; }

        bool IsCloudSyncEnabled { get; }

        bool IsInErrorState { get; }

        string GetLastError();

        /// <summary>
        /// Gets all the available extension types which are not disabled
        /// </summary>
        /// <returns></returns>
        IEnumerable<Type> GetAvailableExtensionTypes();

        event EventHandler NetworkAvailabilityChanged;

        event EventHandler<AuthenticationStageEventArgs> AuthenticationStateChanged;

        event EventHandler<ErrorEventArgs> ErrorOccurred;

        event EventHandler<SyncEventArgs> ItemSynced;
    }
}
