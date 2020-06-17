using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Win32;
using ClipboxGuard.Services;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace ClipboxGuard
{
    public class Bootstrapper
    {
        private int _shutdownSequenceCallCount;
        private bool _isStartedFromSvc;

        private IPersistenceProvider _persistenceProvider;
        private MainProcessKeeperService _mainProcessKeeperService;
        private SearchProcessKeeperService _searchProcessKeeperService;
        private IErrorMessageWindowService _errorMessageWindowService;
        private ISessionMonitorService _sessionMonitorService;


        public void PerformStartupSequence()
        {
            string[] startupArgs = Environment.GetCommandLineArgs();
            _isStartedFromSvc = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SERVICE_ARG);
            bool isStartedFromWindowsScheduler = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SCHEDULER_ARG);
            bool isStartedFromGuard = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_GUARD_ARG);

            if (!_isStartedFromSvc && !isStartedFromWindowsScheduler && !isStartedFromGuard)
            {
                int mySessionId = SessionUtils.GetCurrentSessionId();

                if (ProcessUtils.CheckProcessExists(Path.GetFileNameWithoutExtension(ProductConstants.SERVICE_EXE_NAME)))
                {
                    DemandStartFromService(mySessionId);
                }
                else
                {
                    MessageBox.Show("This executable is not meant to be started by the user."); 
                }

                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            string installationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            installationPath = new Uri(installationPath).LocalPath;
            _persistenceProvider = new AppDataPersistenceProvider();

            int sessionId = SessionUtils.GetCurrentSessionId();

            _searchProcessKeeperService = new SearchProcessKeeperService(_persistenceProvider, installationPath, sessionId);
            _mainProcessKeeperService = new MainProcessKeeperService(_searchProcessKeeperService, _persistenceProvider, installationPath, sessionId);
            _errorMessageWindowService = new ErrorMessageWindowService(_mainProcessKeeperService, _searchProcessKeeperService, _isStartedFromSvc);
            _sessionMonitorService = new SessionMonitorService(_mainProcessKeeperService, _searchProcessKeeperService);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            bool isRestartRequested = false;
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    _persistenceProvider.Write(ProductConstants.LAST_GUARD_EXCEPTION_PERSISTENCE_KEY, ex.ToString());
                }

                _errorMessageWindowService?.ShowErrorMessageWindow(out isRestartRequested);
            }
            catch { }

            PerformShutdownSequence(isResultOfError: true);

            if (isRestartRequested && !_isStartedFromSvc)
            {
                using (Process currentProcess = Process.GetCurrentProcess())
                using (Process newProcess = new Process())
                {
                    newProcess.StartInfo = new ProcessStartInfo(currentProcess.MainModule.FileName, ProductConstants.STARTED_FROM_GUARD_ARG);
                    newProcess.Start();
                }
            }

            Environment.Exit((int)ProcessExitCodes.Error);
        }

        public void PerformShutdownSequence(bool isResultOfError)
        {
            if (Interlocked.Increment(ref _shutdownSequenceCallCount) == 1)
            {
                TryDisposeObject(_mainProcessKeeperService);
                TryDisposeObject(_searchProcessKeeperService);
            }
        }

        private void DemandStartFromService(int mySessionId)
        {
            // send message to the service to start us elevated
            NamedPipesClient npClient = new NamedPipesClient(ProductConstants.SERVICE_NAMED_PIPE);
            npClient.SendMessage(mySessionId.ToString());
        }

        private void ShowErrorWindow()
        {
            //string lastException = _persistenceProvider.ReadText(ProductConstants.LAST_EXCEPTION_PERSISTENCE_KEY);

            //Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)(() =>
            //{
            //    var errorWindow = new ErrorWindow(lastException);
            //    errorWindow.Closed += ErrorWinow_Closed;
            //}));
        }

        private void ErrorWinow_Closed(object sender, EventArgs e)
        {
            //_persistenceProvider.Write(ProductConstants.LAST_EXCEPTION_PERSISTENCE_KEY, new byte[0]);

            //var errorWindow = (ErrorWindow)sender;
            //if (_isStartedFromService)
            //{
            //    Shutdown
            //        (
            //            // if the 'restart' checkbox is checked,
            //            // tell the service that it should start the client process
            //            // by exitting with 'Special' exit code
            //            errorWindow.IsRestartClientProcessCbChecked ?
            //                (int)ProcessExitCodes.Special :
            //                (int)ProcessExitCodes.Ok
            //        );
            //}
        }
        
        private void TryDisposeObject(IDisposable obj)
        {
            if (obj != null)
            {
                try { obj.Dispose(); } catch { }
            }
        }
    }
}
