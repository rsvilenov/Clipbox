using ClipboxGuard.Services.EventArgsClasses;
using System;

namespace ClipboxGuard.Services.Interfaces
{
    public interface IMainProcessKeeperService
    {
        event EventHandler<RestartFatalErrorEventArgs> FatalErrorOccurred;
        void StartExternalProcessManagement();
        void StopMainProcess();
    }
}
