using Clipbox.Core.Enums;
using ClipboxGuard.Services.Interfaces;
using System.Windows;

namespace ClipboxGuard.Services
{
    public class SessionMonitorService : ISessionMonitorService
    {
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;

        public SessionMonitorService(IMainProcessKeeperService mainProcessKeeperService,
            ISearchProcessKeeperService searchProcessKeeperService)
        {
            _mainProcessKeeperService = mainProcessKeeperService;
            _searchProcessKeeperService = searchProcessKeeperService;

            Application.Current.SessionEnding += Current_SessionEnding;
        }

        private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            // stop the main process first, because it depends on the search process
            _mainProcessKeeperService.StopSearchProcess();
            _searchProcessKeeperService.StopSearchProcess();
            // now stop the guard
            Application.Current.Shutdown((int)ProcessExitCodes.Ok);
        }
    }
}
