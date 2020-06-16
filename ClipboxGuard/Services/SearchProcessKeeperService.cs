using Clipbox.Core;
using Clipbox.Core.Enums;
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
using System.Text;
using System.Threading;

namespace ClipboxGuard.Services
{
    public class SearchProcessKeeperService : ISearchProcessKeeperService, IDisposable
    {
        private const int WATCHER_POLL_INTERVAL_MS = 500;

        private readonly IPersistenceProvider _persistenceProvider;
        private readonly string _installationPath;
        private readonly int _sessionId;

        private bool _isFlagWatcherActive;
        private bool _flagForExternalProcessExists;
        private ManualResetEvent _watcherMre;
        private readonly object _watcherMreLockHandle;
        private Process _searchProcess;
        private StringBuilder _processStdErrStrBuilder;
        private bool _isStdErrSubscribed;
        private RemoteSearch _remoteSearchClient;

        public event EventHandler RestartOfMainProcessRequested;
        public event EventHandler<FatalErrorEventArgs> FatalErrorOccurred;
        
        public SearchProcessKeeperService(IPersistenceProvider persistenceProvider,
            string installationPath,
            int sessionId)
        {
            _persistenceProvider = persistenceProvider;
            _installationPath = installationPath;
            _sessionId = sessionId;

            _flagForExternalProcessExists = _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY);
            _watcherMre = new ManualResetEvent(false);
            _watcherMreLockHandle = new object();
            _remoteSearchClient = new RemoteSearch(ProductConstants.SEARCH_SERVICE_NAMED_PIPE_PREFIX, true);

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

            _processStdErrStrBuilder = new StringBuilder();

            _searchProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = searchProcessPath,
                    RedirectStandardError = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true,
                PriorityClass = ProcessPriorityClass.Normal
            };

            _searchProcess.Exited += _searchProcess_Exited;
            _searchProcess.ErrorDataReceived += _searchProcess_ErrorDataReceived;
            _searchProcess.Start();
            // begin async read
            _searchProcess.BeginErrorReadLine();
            _isStdErrSubscribed = true;
        }

        private void _searchProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                _searchProcess.ErrorDataReceived -= _searchProcess_ErrorDataReceived;
                _isStdErrSubscribed = false;
            }
            else
            {
                _processStdErrStrBuilder.AppendLine(e.Data);
            }
        }

        private void _searchProcess_Exited(object sender, EventArgs e)
        {
            _searchProcess.Exited -= _searchProcess_Exited;

            if (_isStdErrSubscribed)
            {
                _searchProcess.ErrorDataReceived -= _searchProcess_ErrorDataReceived;
                _isStdErrSubscribed = false;
            }

            int exitCode = _searchProcess.ExitCode;

            _searchProcess.Dispose();

            if (exitCode == (int)ProcessExitCodes.Ok)
            {
                return;
            }

            if (_persistenceProvider.CheckIfCategoryExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY))
            {
                IEnumerable<string> errorLogs = _persistenceProvider.ListKeys(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY);

                int errorLogsCount = errorLogs.Count();
                if (errorLogsCount == 0)
                {
                    string processStdErr = _processStdErrStrBuilder.ToString();
                    FatalErrorOccurred?.Invoke(this, new FatalErrorEventArgs(processStdErr));
                    return;
                }
                
                if (errorLogsCount > 3)
                {
                    if (!_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE))
                    {
                        throw new InvalidOperationException("No aggregate error key was found. Please check the logging code of the search provider.");
                    }

                    string aggregateErrorText = _persistenceProvider.ReadText(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE);
                    FatalErrorOccurred?.Invoke(this, new FatalErrorEventArgs(aggregateErrorText));
                    return;
                }

                if (_flagForExternalProcessExists)
                {
                    StartSearchProcess();
                }
            }
        }

        private void SetUpFlagWatcher()
        {
            // we can't use FileSystemWatcher, because our persistence layer
            // is abstracted behind an interface and we don't know whether
            // the actual storage media is a filesystem.
            _isFlagWatcherActive = true;
            while (_isFlagWatcherActive)
            {
                bool flagStillExists = _persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY);
                
                if (_flagForExternalProcessExists != flagStillExists)
                {
                    // if flagStillExists == false, stop the search process
                    try
                    {
                        StopSearchProcess();
                    }
                    catch(Exception ex)
                    {
                        lock (_watcherMreLockHandle)
                        {
                            _watcherMre.Set();
                        }

                        throw new InvalidOperationException("The search process cannot be stopped.", ex);
                    }

                    _flagForExternalProcessExists = flagStillExists;
                    RestartOfMainProcessRequested?.Invoke(this, new EventArgs());
                }

                Thread.Sleep(WATCHER_POLL_INTERVAL_MS);

                lock (_watcherMreLockHandle)
                {
                    _watcherMre.Set();
                }
            }
        }

        private bool CheckIsSearchProcessRunning()
        {
            string searchProcessPath = Path.Combine(_installationPath, ProductConstants.SEARCH_PROCESS_EXE_NAME);
            IEnumerable<int> pids = ProcessUtils.GetProcessIdsByPathAndSessionId(searchProcessPath, _sessionId);
            return pids.Any();
        }

        private void StopSearchProcess()
        {
            try
            {
                _remoteSearchClient.PostServerQuitRequest();
            }
            catch (TimeoutException)
            {
                if (!CheckIsSearchProcessRunning())
                {
                    // the process has already stopped
                    return;
                }
            }
            try
            {
                _searchProcess.WaitForExit(3000);
                _searchProcess.Kill();

                if (_isStdErrSubscribed)
                {
                    _searchProcess.ErrorDataReceived -= _searchProcess_ErrorDataReceived;
                    _isStdErrSubscribed = false;
                }

                _searchProcess.Dispose();
            }
            catch (InvalidOperationException)
            {
                // the process has already exitted
            }
            catch (Win32Exception)
            {
                // the process is still terminating
                Thread.Sleep(3000);
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

                if (_isStdErrSubscribed)
                {
                    _searchProcess.ErrorDataReceived -= _searchProcess_ErrorDataReceived;
                    _isStdErrSubscribed = false;
                }

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
