using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.Utils;
using ClipboxGuard.Enums;
using ClipboxGuard.EventArgsClasses;
using ClipboxGuard.Services.Common;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ClipboxGuard.Services
{
    public class ErrorWindowService : IErrorWindowService, IDisposable
    {
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;
        private readonly IReportAnalysisService _reportAnalysisService;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly IReportSubmitterService _reportSubmitterService;
        private readonly bool _isStartedFromSvc;
        private readonly ManualResetEvent _mainWindowWaitMre;
        private readonly OwnThreadDispatcher _otDispatcher;

        private static readonly object _mainWindowLockHandle = new object();
        private ErrorWindow _errorWindow;
        private bool _isDisposed;
        public ErrorWindowService(IPersistenceProvider persistenceProvider,
            bool isStartedFromSvc)
        {
            _persistenceProvider = persistenceProvider;
            _isStartedFromSvc = isStartedFromSvc;
            _mainWindowWaitMre = new ManualResetEvent(false);
            _otDispatcher = new OwnThreadDispatcher();
        }

        public ErrorWindowService(IMainProcessKeeperService mainProcessKeeperService,
            ISearchProcessKeeperService searchProcessKeeperService,
            IReportAnalysisService reportAnalysisService,
            IReportSubmitterService reportSubmitterService,
            IPersistenceProvider persistenceProvider,
            bool isStartedFromSvc)
            : this(persistenceProvider, isStartedFromSvc)
        {
            _mainProcessKeeperService = mainProcessKeeperService;
            _searchProcessKeeperService = searchProcessKeeperService;
            _reportAnalysisService = reportAnalysisService;
            _reportSubmitterService = reportSubmitterService;
            _isStartedFromSvc = isStartedFromSvc;
            _persistenceProvider = persistenceProvider;

            _mainProcessKeeperService.FatalErrorOccurred += _mainProcessKeeperService_FatalErrorOccurred;
            _searchProcessKeeperService.FatalErrorOccurred += _searchProcessKeeperService_FatalErrorOccurred;
            if (_reportSubmitterService != null)
            {
                _reportSubmitterService.StateChanged += _reportSubmitterService_StateChanged;
            }
        }

        private void _reportSubmitterService_StateChanged(object sender, ReportStateEventArgs e)
        {
            _errorWindow?.SetReportSubmitState(e.State);
            if (e.State == ReportSubmitState.Error)
            {
                _errorWindow?.SetReportSubmitErrorText(_reportSubmitterService.GetError());
            }
        }

        private void _mainProcessKeeperService_FatalErrorOccurred(object sender, RestartFatalErrorEventArgs e)
        {
            ReportAnalysis analysis = _reportAnalysisService.GetAnalysis();

            bool isRestartProcessRequested;

            // check if there is something logged for the main process
            if (analysis.ErrorOrigin.HasFlag(ErrorOrigin.MainProcess))
            {
                ShowErrorWindow(out isRestartProcessRequested, analysis);
            }
            else
            {
                // if there is nothing logged for the crash of the main process, this means that
                // most likely some low-level winapi related error has brought the process down,
                // and we do now know what it is. So nothing to show to the user and nothing to
                // send to the developers... Just restart the main process then.
                isRestartProcessRequested = true;
            }

            e.RestartProcess = isRestartProcessRequested;

            if (!isRestartProcessRequested)
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

        private void _searchProcessKeeperService_FatalErrorOccurred(object sender, RestartFatalErrorEventArgs e)
        {
            ReportAnalysis analysis = _reportAnalysisService.GetAnalysis();
            ShowErrorWindow(out bool restartProcessRequested, analysis);
            e.RestartProcess = restartProcessRequested;
        }

        private void _searchProcessKeeperService_IndexFatalErrorOccurred(object sender, FatalErrorEventArgs e)
        {
            ReportAnalysis analysis = _reportAnalysisService.GetAnalysis();
            ShowErrorWindow(analysis);
        }

        public void ShowErrorWindow(out bool restartProcessRequested, ReportAnalysis reportAnalysis = null)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ErrorWindowService));
            }

            ShowErrorWindow(true, out restartProcessRequested, reportAnalysis);
        }

        public void ShowErrorWindow(ReportAnalysis reportAnalysis = null)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ErrorWindowService));
            }

            ShowErrorWindow(false, out _, reportAnalysis);
        }

        private void ShowErrorWindow(bool showRestartOption, out bool restartProcessRequested, ReportAnalysis reportAnalysis = null)
        {
            ReportAnalysis analysis = reportAnalysis ?? _reportAnalysisService?.GetAnalysis();
            if (analysis == null)
            {
                throw new ArgumentException($"{nameof(reportAnalysis)} is obligatory if no instance of {nameof(IReportAnalysisService)} is passed to the constructor of this class");
            }

            // show the error windows one by one if more than one error occurs.
            lock (_mainWindowLockHandle)
            {
                _otDispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                {
                    _errorWindow = new ErrorWindow(analysis,
                        analysis.ErrorOrigin,
                        showRestartOption,
                        showSubmitOption: _reportSubmitterService != null);
                    _errorWindow.Closing += _errorWindow_Closing;
                    _errorWindow.SubmitReportRequested += _errorWindow_SubmitReportRequested;
                    _errorWindow.ShowDialog();
                }));

                _mainWindowWaitMre.WaitOne();

                bool restartRequired = false;
                using (ManualResetEvent restartMre = new ManualResetEvent(false))
                {
                    _otDispatcher.Invoke(DispatcherPriority.Send, (Action)(() =>
                    {
                        restartRequired = _errorWindow.IsRestartRequested;
                        restartMre.Set();
                    }));

                    restartMre.WaitOne();
                    restartProcessRequested = restartRequired;
                }
            }
            
        }

        private void _errorWindow_SubmitReportRequested(object sender, RequestSubmitReportEventArgs e)
        {
            if (_reportSubmitterService != null)
            {
                _reportSubmitterService.SubmitReport(e.ReportAnalysis.AggregateReport);
            }
        }

        private void _errorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _errorWindow.Closing -= _errorWindow_Closing;
            _mainWindowWaitMre.Set();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _otDispatcher.Dispose();
                _mainWindowWaitMre.Dispose();
                _mainProcessKeeperService.FatalErrorOccurred -= _mainProcessKeeperService_FatalErrorOccurred;
                _searchProcessKeeperService.FatalErrorOccurred -= _searchProcessKeeperService_FatalErrorOccurred;
                if (_reportSubmitterService != null)
                {
                    _reportSubmitterService.StateChanged -= _reportSubmitterService_StateChanged;
                }
            }
        }
    }
}
