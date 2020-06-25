using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Diagnostics;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Localization.LozalizationProviders;
using Clipbox.Win32;
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

            ILocalizationProvider localizationProvider = AsmAwareResxLocalizationProvider.Instance;

            TranslationSourcePool.InitTranslationSourcePool(localizationProvider);

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
                _errorMessageWindowService = new ErrorWindowService(_mainProcessKeeperService, _searchProcessKeeperService, _reportAnalysisService, _persistenceProvider, _isStartedFromSvc);
                _sessionMonitorService = new SessionMonitorService(_mainProcessKeeperService, _searchProcessKeeperService);

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
                
                _errorMessageWindowService?.ShowErrorWindow(out isRestartRequested, new ReportAnalysis(ErrorOrigin.Unknown, exception.ToString()));
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
            }
        }

        private void DemandStartFromService(int mySessionId)
        {
            // send message to the service to start us elevated
            NamedPipesClient npClient = new NamedPipesClient(ProductConstants.SERVICE_NAMED_PIPE);
            npClient.SendMessage(mySessionId.ToString());
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
