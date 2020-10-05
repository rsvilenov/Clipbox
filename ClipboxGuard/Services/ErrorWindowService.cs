using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.Utils;
using ClipboxGuard.Services.Common;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ClipboxGuard.Services
{
    public class ErrorWindowService : IErrorWindowService
    {
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;
        private readonly IReportAnalysisService _reportAnalysisService;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly bool _isStartedFromSvc;

        private static readonly object _mainWindowLockHandle = new object();

        public ErrorWindowService(IPersistenceProvider persistenceProvider,
            bool isStartedFromSvc)
        {
            _persistenceProvider = persistenceProvider;
            _isStartedFromSvc = isStartedFromSvc;
        }

        public ErrorWindowService(IMainProcessKeeperService mainProcessKeeperService,
            ISearchProcessKeeperService searchProcessKeeperService,
            IReportAnalysisService reportAnalysisService,
            IPersistenceProvider persistenceProvider,
            bool isStartedFromSvc)
            : this(persistenceProvider, isStartedFromSvc)
        {
            _mainProcessKeeperService = mainProcessKeeperService;
            _searchProcessKeeperService = searchProcessKeeperService;
            _reportAnalysisService = reportAnalysisService;
            _isStartedFromSvc = isStartedFromSvc;
            _persistenceProvider = persistenceProvider;

            _mainProcessKeeperService.FatalErrorOccurred += _mainProcessKeeperService_FatalErrorOccurred;
            _searchProcessKeeperService.FatalErrorOccurred += _searchProcessKeeperService_FatalErrorOccurred;

        }

        private void _mainProcessKeeperService_FatalErrorOccurred(object sender, RestartFatalErrorEventArgs e)
        {
            ReportAnalysis analysis = _reportAnalysisService.GetAnalysis();

            ShowErrorWindow(out bool restartProcessRequested, analysis);
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
            ShowErrorWindow(true, out restartProcessRequested, reportAnalysis);
        }

        public void ShowErrorWindow(ReportAnalysis reportAnalysis = null)
        {
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
                using (OwnThreadDispatcher otDispatcher = new OwnThreadDispatcher())
                {
                    ErrorWindow errorWindow = null;
                    using (ManualResetEvent mainWindowWaitMre = new ManualResetEvent(false))
                    {
                        otDispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                        {
                            errorWindow = new ErrorWindow(analysis.ErrorDetails, 
                                analysis.ErrorOrigin,
                                showRestartOption);
                            errorWindow.Closing += (e, a) => mainWindowWaitMre.Set();
                            errorWindow.ShowDialog();
                        }));

                        mainWindowWaitMre.WaitOne();
                    }

                    bool restartRequired = false;
                    using (ManualResetEvent restartMre = new ManualResetEvent(false))
                    {
                        otDispatcher.Invoke(DispatcherPriority.Send, (Action)(() =>
                        {
                            restartRequired = errorWindow.IsRestartRequested;
                            restartMre.Set();
                        }));

                        restartMre.WaitOne();
                        restartProcessRequested = restartRequired;
                    }
                }
            }
            
        }
    }
}
