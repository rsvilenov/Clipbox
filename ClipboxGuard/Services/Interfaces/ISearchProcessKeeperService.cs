using ClipboxGuard.Services.EventArgsClasses;
using System;

namespace ClipboxGuard.Services.Interfaces
{
    public interface ISearchProcessKeeperService
    {
        event EventHandler RestartOfMainProcessRequested;
        event EventHandler<FatalErrorEventArgs> FatalErrorOccurred;
        void StopSearchProcess();
    }
}
