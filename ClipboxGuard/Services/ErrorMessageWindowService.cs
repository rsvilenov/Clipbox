using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;

namespace ClipboxGuard.Services
{
    public class ErrorMessageWindowService : IErrorMessageWindowService
    {
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;

        public ErrorMessageWindowService(IMainProcessKeeperService mainProcessKeeperService,
            ISearchProcessKeeperService searchProcessKeeperService)
        {
            _mainProcessKeeperService = mainProcessKeeperService;
            _searchProcessKeeperService = searchProcessKeeperService;

            _mainProcessKeeperService.FatalErrorOccurred += _mainProcessKeeperService_FatalErrorOccurred;
            _searchProcessKeeperService.FatalErrorOccurred += _searchProcessKeeperService_FatalErrorOccurred;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Show the error dialog and when the user clicks ok, let the process die by itself.
            // The windows service (ClipboxSvc) should restart the process. 
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
