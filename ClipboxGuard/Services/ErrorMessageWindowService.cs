using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;

namespace ClipboxGuard.Services
{
    public class ErrorMessageWindowService : IErrorMessageWindowService
    {
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;
        private readonly bool _isStartedFromSvc;

        public ErrorMessageWindowService(IMainProcessKeeperService mainProcessKeeperService,
            ISearchProcessKeeperService searchProcessKeeperService,
            bool isStartedFromSvc)
        {
            _mainProcessKeeperService = mainProcessKeeperService;
            _searchProcessKeeperService = searchProcessKeeperService;
            _isStartedFromSvc = isStartedFromSvc;

            _mainProcessKeeperService.FatalErrorOccurred += _mainProcessKeeperService_FatalErrorOccurred;
            _searchProcessKeeperService.FatalErrorOccurred += _searchProcessKeeperService_FatalErrorOccurred;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public void ShowErrorMessageWindow(out bool isRestartRequested)
        {
            isRestartRequested = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Show the error dialog and when the user clicks ok, let the process die by itself.
            // The windows service (ClipboxSvc) should restart the process. 
            // If the service is not available, start the process anew here.
            // If the user unchecks the "restart" checkbox, exit this process with code 0,
            // so that the service will know that it should not restart it.
        }

        private void _mainProcessKeeperService_FatalErrorOccurred(object sender, RestartFatalErrorEventArgs e)
        {
            // show the window, check whether the user has unchecked the "Restart" checkbox
            // and if so, set the event args restart flag to false
        }

        private void _searchProcessKeeperService_FatalErrorOccurred(object sender, EventArgsClasses.FatalErrorEventArgs e)
        {
            // try to get the aggregate log file and show the window
        }
    }
}
