using ClipboxGuard.Services.EventArgsClasses;
using System;

namespace ClipboxGuard.Services.Interfaces
{
    public interface ISearchProcessKeeperService
    {
        event EventHandler RestartOfMainProcessRequested;
        event EventHandler<RestartFatalErrorEventArgs> FatalErrorOccurred;
        event EventHandler<FatalErrorEventArgs> IndexFatalErrorOccurred;
        void StartExternalProcessManagement();
        void StopSearchProcess();
    }
}
