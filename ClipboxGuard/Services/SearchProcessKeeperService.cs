using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Search.SearchProviders;
using Clipbox.Win32;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClipboxGuard.Services
{
    public class SearchProcessKeeperService : ISearchProcessKeeperService, IDisposable
    {
        private const int WATCHER_POLL_INTERVAL_MS = 500;
        private const int SEARCH_PROCESS_EXIT_TIMEOUT_MS = 3000;

        private readonly IPersistenceProvider _persistenceProvider;
        private readonly string _installationPath;
        private readonly int _sessionId;

        private bool _isFlagWatcherActive;
        private bool _flagForExternalProcessExists;
        private ManualResetEvent _watcherMre;
        private readonly object _watcherMreLockHandle;
        private Process _searchProcess;
        private RemoteSearch _remoteSearchClient;
        private NamedPipesClient _pipeClient;

        public event EventHandler RestartOfMainProcessRequested;
        public event EventHandler<RestartFatalErrorEventArgs> FatalErrorOccurred;
        public event EventHandler<FatalErrorEventArgs> IndexFatalErrorOccurred;

        public SearchProcessKeeperService(IPersistenceProvider persistenceProvider,
            string installationPath,
            int sessionId)
        {
            _persistenceProvider = persistenceProvider;
            _installationPath = installationPath;
            _sessionId = sessionId;

            _pipeClient = new NamedPipesClient($"{ProductConstants.GUARD_NAMED_PIPE_PREFIX}.{sessionId}");

            _flagForExternalProcessExists = _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY);
            _watcherMre = new ManualResetEvent(false);
            _watcherMreLockHandle = new object();
            _remoteSearchClient = new RemoteSearch(ProductConstants.SEARCH_SERVICE_NAMED_PIPE_PREFIX, true);
        }

        public void StartExternalProcessManagement()
        {
            StartFlagWatcher();

            if (_flagForExternalProcessExists)
            {
                StartSearchProcess();
            }
        }

        private void StartSearchProcess()
        {
            string searchProcessPath = Path.Combine(_installationPath, ProductConstants.SEARCH_PROCESS_EXE_NAME);
            if (!File.Exists(searchProcessPath))
            {
                throw new FileNotFoundException(searchProcessPath);
            }

            if (CheckIsSearchProcessRunning())
            {
                throw new InvalidOperationException($"Search process is already started in the current session. (session id: {_sessionId})");
            }
            
            _searchProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = searchProcessPath,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            _searchProcess.Exited += _searchProcess_Exited;
            _searchProcess.Start();
            _searchProcess.EnableRaisingEvents = true;
            _searchProcess.PriorityClass = ProcessPriorityClass.Normal;
        }

        private void _searchProcess_Exited(object sender, EventArgs e)
        {
            _searchProcess.Exited -= _searchProcess_Exited;

            int exitCode = _searchProcess.ExitCode;
            string stdErr = _searchProcess.StandardError.ReadToEnd();

            try { _persistenceProvider.Write(ProductConstants.SEARCH_CRASHFILE_PERSISTENCE_KEY, stdErr); }
            catch { }

            _searchProcess.Dispose();

            if (exitCode == (int)ProcessExitCodes.Ok)
            {
                return;
            }

            int indexErrorCount = 0;
            if (_persistenceProvider.CheckIfCategoryExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY))
            {
                IEnumerable<string> indexErrorLogs = _persistenceProvider.ListKeys(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY);
                indexErrorCount = indexErrorLogs?.Count() ?? 0;
            }

            if (indexErrorCount == 0)
            {
                var args = new RestartFatalErrorEventArgs(stdErr);
                FatalErrorOccurred?.Invoke(this, args);

                if (args.RestartProcess)
                {
                    StartSearchProcess();
                }
                else
                {
                    DisableSearchInMainProcess();
                }

                return;
            }

            if (indexErrorCount > 3)
            {
                if (!_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE))
                {
                    throw new InvalidOperationException("No aggregate error key was found. Please check the logging code of the search provider.");
                }

                string aggregateErrorText = _persistenceProvider.ReadText(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE);
                IndexFatalErrorOccurred?.Invoke(this, new FatalErrorEventArgs(aggregateErrorText));

                DisableSearchInMainProcess();

                return;
            }

            if (indexErrorCount > 0
                && indexErrorCount <= 3
                && _flagForExternalProcessExists)
            {
                StartSearchProcess();
            }
        }

        private void DisableSearchInMainProcess()
        {
            try
            {
                _pipeClient.SendMessage(ProductConstants.PIPE_COMMAND_DISABLE_SEARCH);
            }
            // TODO: demand restart of the main process here
            catch (TimeoutException)
            { }
            catch (IOException)
            { }
        }

        private void StartFlagWatcher()
        {
            // we can't use FileSystemWatcher, because our persistence layer
            // is abstracted behind an interface and we don't know whether
            // the actual storage media is a filesystem.
            _isFlagWatcherActive = true;
            Task.Run(() =>
            {
                try
                {
                    while (_isFlagWatcherActive)
                    {
                        bool flagStillExists = _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY);

                        if (_flagForExternalProcessExists != flagStillExists)
                        {
                            _flagForExternalProcessExists = flagStillExists;

                            if (_flagForExternalProcessExists)
                            {
                                StartSearchProcess();
                            }
                            else
                            {
                                try
                                {
                                    StopSearchProcess();
                                }
                                catch (Exception ex)
                                {
                                    lock (_watcherMreLockHandle)
                                    {
                                        _watcherMre.Set();
                                    }

                                    throw new InvalidOperationException("The search process cannot be stopped.", ex);
                                }
                            }

                            RestartOfMainProcessRequested?.Invoke(this, new EventArgs());
                        }

                        Thread.Sleep(WATCHER_POLL_INTERVAL_MS);
                    }
                }
                finally
                {
                    lock (_watcherMreLockHandle)
                    {
                        _watcherMre.Set();
                    }
                }
            });
        }

        private bool CheckIsSearchProcessRunning()
        {
            string searchProcessPath = Path.Combine(_installationPath, ProductConstants.SEARCH_PROCESS_EXE_NAME);
            IEnumerable<int> pids = ProcessUtils.GetProcessIdsByPathAndSessionId(searchProcessPath, _sessionId);
            return pids.Any();
        }

        public void StopSearchProcess()
        {
            if (!CheckIsSearchProcessRunning())
            {
                return;
            }

            try
            {
                _remoteSearchClient.PostServerQuitRequest();
            }
            catch (Exception ex) when (ex is TimeoutException || ex is IOException)
            {
                if (!CheckIsSearchProcessRunning())
                {
                    // the has stopped in the meantime
                    return;
                }
            }
            try
            {
                _searchProcess.WaitForExit(SEARCH_PROCESS_EXIT_TIMEOUT_MS);
                _searchProcess.Kill();

                _searchProcess.Dispose();
            }
            catch (InvalidOperationException)
            {
                // the process has already exitted 
                // (see https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=netcore-3.1)
            }
            catch (Win32Exception)
            {
                // the process is probably still terminating
                Thread.Sleep(SEARCH_PROCESS_EXIT_TIMEOUT_MS);
                if (CheckIsSearchProcessRunning())
                {
                    throw new InvalidOperationException("Search process could not be stopped.");
                }

                _searchProcess.Dispose();
            }
        }
        
        public void Dispose()
        {
            try
            {
                StopSearchProcess();

                lock (_watcherMreLockHandle)
                {
                    _watcherMre.Reset();
                    _isFlagWatcherActive = false;
                }

                _watcherMre.WaitOne();
            }
            catch { }
            finally
            {
                _watcherMre.Dispose();
            }
        }
    }
}
