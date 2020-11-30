using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Diagnostics;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.RegistryAccess;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Localization.LozalizationProviders;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using ClipboxGuard.Enums;
using ClipboxGuard.Services;
using ClipboxGuard.Services.Common;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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
        private IErrorWindowService _errorMessageWindowService;
        private ISessionMonitorService _sessionMonitorService;
        private IReportAnalysisService _reportAnalysisService;
        private ReportSubmitterService _reportSubmitterService;
        private ClipboardCheckerService _clipboardCheckerService;

        public void PerformStartupSequence()
        {
            string[] startupArgs = Environment.GetCommandLineArgs();
            _isStartedFromSvc = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SERVICE_ARG);
            bool isStartedFromWindowsScheduler = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SCHEDULER_ARG);
            bool isStartedFromGuard = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_GUARD_ARG);
            bool isStartedFromShortcut = startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SHORTCUT);
            bool isOpenSettingsWindowRequested = startupArgs.Any(arg => arg == ProductConstants.OPEN_SETTINGS_WINDOW_ARG);
            bool isDisableClipboardTrackingRequested = startupArgs.Any(arg => arg == ProductConstants.DISABLE_CLIPOARD_TRAKING_ARG);
            bool isStoreQvStateRequested = startupArgs.Any(arg => arg == ProductConstants.STORE_QV_STATE_ARG);

            int mySessionId = SessionUtils.GetCurrentSessionId();
            if (isStartedFromShortcut)
            {
                int myPid = 0;
                using (Process p = Process.GetCurrentProcess())
                {
                    myPid = p.Id;
                }

                if (ProcessUtils.CheckProcessExistsInSession(Path.GetFileNameWithoutExtension(ProductConstants.GUARD_EXE_NAME), mySessionId, new int[] { myPid }))
                {
                    AskMainProcessToOpenMainWindow(mySessionId);
                }
                else if (ProcessUtils.CheckProcessExists(Path.GetFileNameWithoutExtension(ProductConstants.SERVICE_EXE_NAME)))
                {
                    DemandStartFromService(mySessionId);
                }
                else
                {
                    MessageBox.Show($"{ProductConstants.SERVICE_SC_NAME} is not running.", ProductConstants.PRODUCT_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }
            else if (isOpenSettingsWindowRequested)
            {
                AskMainProcessToOpenSettingsWindow(mySessionId);

                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }
            else if (isDisableClipboardTrackingRequested)
            {
                AskMainProcessToDisableClipboardTracking(mySessionId);

                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }
            else if (isStoreQvStateRequested)
            {
                AskMainProcessToStoreQvState(mySessionId);

                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }
            else if (!_isStartedFromSvc && !isStartedFromWindowsScheduler && !isStartedFromGuard)
            {
                MessageBox.Show("This executable is not meant to be started by the user.");

                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }

            ILocalizationProvider localizationProvider = AsmAwareResxLocalizationProvider.Instance;

            TranslationSourcePool.InitTranslationSourcePool(localizationProvider);

            string cultureCode = null;
            try { cultureCode = RegistryUtils.GetCultureCode(); }
            catch { }

            if (!string.IsNullOrEmpty(cultureCode))
            {
                localizationProvider.CurrentCulture = new System.Globalization.CultureInfo(cultureCode);
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            SynchronizationContext mainSyncContext = SynchronizationContext.Current;
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                var edi = ExceptionDispatchInfo.Capture(args.Exception);
                mainSyncContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
            };

            try
            {
                _persistenceProvider = new AppDataPersistenceProvider();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The persistence provider cannot be initialized: {ex.Message}");
                Application.Current.Shutdown((int)ProcessExitCodes.Error);
                return;
            }

            try
            {
                ClearLastExceptionInfo();

                string installationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
                installationPath = new Uri(installationPath).LocalPath;

                int sessionId = SessionUtils.GetCurrentSessionId();

                _searchProcessKeeperService = new SearchProcessKeeperService(_persistenceProvider, installationPath, sessionId);
                _mainProcessKeeperService = new MainProcessKeeperService(_searchProcessKeeperService, _persistenceProvider, installationPath, sessionId);
                _reportAnalysisService = new ReportAnalysisService(_persistenceProvider);
                _reportSubmitterService = new ReportSubmitterService();
                _errorMessageWindowService = new ErrorWindowService(_mainProcessKeeperService, _searchProcessKeeperService, _reportAnalysisService, _reportSubmitterService, _persistenceProvider, _isStartedFromSvc);
                _sessionMonitorService = new SessionMonitorService(_mainProcessKeeperService, _searchProcessKeeperService);
                _clipboardCheckerService = new ClipboardCheckerService(_mainProcessKeeperService);

                // start the external processes after the error message window service
                // has been started, so that if they crash immediately after starting,
                // their errors will be shown in the error window
                _searchProcessKeeperService.StartExternalProcessManagement();
                _mainProcessKeeperService.StartExternalProcessManagement();
            }
            catch
            {
                _errorMessageWindowService = new ErrorWindowService(_persistenceProvider, _isStartedFromSvc);
                throw;
            }
        }

        private void ClearLastExceptionInfo()
        {
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY))
            {
                _persistenceProvider.Delete(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY);
            }
        }

        private int _numOfUnhandledExceptions;
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref _numOfUnhandledExceptions) > 1)
            {
                return;
            }

            bool isRestartRequested = false;
            try
            {
                Exception exception = (Exception)e.ExceptionObject;

                DiagnosticsReport dr = DiagnosticsReportGenerator
                    .Generate(exception, includeExtensionInfo: false);
                string reportXml = XmlSerialization.Serialize(dr);
                
                try
                {
                    _persistenceProvider.Write(ProductConstants.GUARD_FAILURE_XML_PERSISTENCE_KEY, reportXml);
                }
                catch { }

                AggregateReport ar = AggregateReportGenerator.CreateNewEmptyReport(ProductVersionHelper.GetFullProductVersion());
                ar.GuardProcessReport = dr;

                _errorMessageWindowService?.ShowErrorWindow(out isRestartRequested, new ReportAnalysis(ErrorOrigin.GuardProcess, exception.ToString(), ar));
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

            // if we exit with Special, the service will restart us.
            Environment.Exit(isRestartRequested ? 
                (int)ProcessExitCodes.Special : (int)ProcessExitCodes.Error);
        }

        public void PerformShutdownSequence(bool isResultOfError)
        {
            if (Interlocked.Increment(ref _shutdownSequenceCallCount) == 1)
            {
                TryDisposeObject(_mainProcessKeeperService);
                TryDisposeObject(_searchProcessKeeperService);
                TryDisposeObject(_clipboardCheckerService);
                TryDisposeObject(_reportSubmitterService);
            }
        }

        private void DemandStartFromService(int mySessionId)
        {
            // send message to the service to start us elevated
            NamedPipesClient npClient = new NamedPipesClient(ProductConstants.SERVICE_NAMED_PIPE);
            npClient.SendMessage(mySessionId.ToString());
        }

        private void AskMainProcessToOpenMainWindow(int sessionId)
        {
            NamedPipesClient npClient = new NamedPipesClient($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");
            npClient.SendMessage(ProductConstants.PIPE_COMMAND_OPEN_MAIN_WINDOW);
        }

        private void AskMainProcessToOpenSettingsWindow(int sessionId)
        {
            NamedPipesClient npClient = new NamedPipesClient($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");
            npClient.SendMessage(ProductConstants.PIPE_COMMAND_OPEN_SETTINGS_WINDOW);
        }

        private void AskMainProcessToDisableClipboardTracking(int sessionId)
        {
            NamedPipesClient npClient = new NamedPipesClient($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");
            npClient.SendMessage(ProductConstants.PIPE_COMMAND_DISABLE_TRACKING);
        }

        private void AskMainProcessToStoreQvState(int sessionId)
        {
            NamedPipesClient npClient = new NamedPipesClient($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");
            npClient.SendMessage(ProductConstants.PIPE_COMMAND_STORE_QV_STATE);
        }

        private void TryDisposeObject(IDisposable obj)
        {
            if (obj != null)
            {
                // Dispose() should never throw, but anyways...
                try { obj.Dispose(); } catch { }
            }
        }
    }
}
