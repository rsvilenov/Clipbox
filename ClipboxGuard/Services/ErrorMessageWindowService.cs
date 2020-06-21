using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.Utils;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ClipboxGuard.Services
{
    public class ErrorMessageWindowService : IErrorMessageWindowService
    {
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly bool _isStartedFromSvc;

        private readonly AutoResetEvent _mainWindowWaitMre;

        public ErrorMessageWindowService(IPersistenceProvider persistenceProvider,
            bool isStartedFromSvc)
        {
            _persistenceProvider = persistenceProvider;
            _isStartedFromSvc = isStartedFromSvc;

            _mainWindowWaitMre = new AutoResetEvent(true);
        }

        public ErrorMessageWindowService(IMainProcessKeeperService mainProcessKeeperService,
            ISearchProcessKeeperService searchProcessKeeperService,
            IPersistenceProvider persistenceProvider,
            bool isStartedFromSvc)
        {
            _mainProcessKeeperService = mainProcessKeeperService;
            _searchProcessKeeperService = searchProcessKeeperService;
            _isStartedFromSvc = isStartedFromSvc;
            _persistenceProvider = persistenceProvider;

            _mainProcessKeeperService.FatalErrorOccurred += _mainProcessKeeperService_FatalErrorOccurred;
            _searchProcessKeeperService.FatalErrorOccurred += _searchProcessKeeperService_FatalErrorOccurred;

        }

        public void ShowErrorMessageWindow(out bool isRestartRequested)
        {
            StringBuilder lastExceptionsStringBuilder = new StringBuilder();
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.LAST_EXCEPTION_PERSISTENCE_KEY))
            {
                lastExceptionsStringBuilder.AppendLine("Main process exception:");
                lastExceptionsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.LAST_EXCEPTION_PERSISTENCE_KEY));
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.LAST_GUARD_EXCEPTION_PERSISTENCE_KEY))
            {
                lastExceptionsStringBuilder.AppendLine("Guard process exception:");
                lastExceptionsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.LAST_GUARD_EXCEPTION_PERSISTENCE_KEY));
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY))
            {
                lastExceptionsStringBuilder.AppendLine("Main crash error:");
                lastExceptionsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY));
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY))
            {
                lastExceptionsStringBuilder.AppendLine("Search crash error:");
                lastExceptionsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY));
            }

            if (_persistenceProvider.CheckIfCategoryExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY)
                && _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE))
            {
                lastExceptionsStringBuilder.AppendLine("Search index exception:");
                lastExceptionsStringBuilder.Append(_persistenceProvider.ReadText(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE));
            }

            ShowErrorWindow(lastExceptionsStringBuilder.ToString(), out bool restartProcessRequested);

            isRestartRequested = restartProcessRequested;
        }

        private void _mainProcessKeeperService_FatalErrorOccurred(object sender, RestartFatalErrorEventArgs e)
        {
            // show the window, check whether the user has unchecked the "Restart" checkbox
            // and if so, set the event args restart flag to false
            string lastException = _persistenceProvider.ReadText(ProductConstants.LAST_EXCEPTION_PERSISTENCE_KEY);
            ShowErrorWindow(lastException, out bool restartProcessRequested);
            e.RestartProcess = restartProcessRequested;

            if (!restartProcessRequested)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown
                      (
                               (int)ProcessExitCodes.Ok
                      );
                });
            }
        }

        private void _searchProcessKeeperService_FatalErrorOccurred(object sender, FatalErrorEventArgs e)
        {
            ShowErrorWindow($"{e.ErrorText}{Environment.NewLine}{e?.Exception?.ToString()}", out _);
        }

        private void ShowErrorWindow(string errorText, out bool restartProcessRequested)
        {
            // show the error windows one by one if more than one error occurs.
            _mainWindowWaitMre.WaitOne();

            using (OwnThreadDispatcher otDispatcher = new OwnThreadDispatcher())
            {
                ErrorWindow errorWindow = null;
                otDispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                {
                    errorWindow = new ErrorWindow(errorText);
                    errorWindow.Closed += (e, a) => _mainWindowWaitMre.Set();
                    errorWindow.ShowDialog();
                }));

                _mainWindowWaitMre.WaitOne();
                restartProcessRequested = errorWindow.IsRestartClientProcessCbChecked;
            }
        }
    }
}
