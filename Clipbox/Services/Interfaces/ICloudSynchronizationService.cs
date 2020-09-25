using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;

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

        string GetLastErrorDetails();

        void RequestAuthentication();

        void RequestLoginChange();
        void ClearSyncedData();

        /// <summary>
        /// Gets all the available extension types which are not disabled
        /// </summary>
        /// <returns></returns>
        IEnumerable<Type> GetAvailableExtensionTypes();

        event EventHandler NetworkAvailabilityChanged;

        event EventHandler<AuthenticationStageEventArgs> AuthenticationStateChanged;

        event EventHandler<ErrorStateEventArgs> ErrorStateChanged;

        event EventHandler<SyncEventArgs> ItemSynced;
    }
}
