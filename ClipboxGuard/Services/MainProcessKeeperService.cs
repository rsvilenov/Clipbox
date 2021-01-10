using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Win32;
using ClipboxGuard.Services.EventArgsClasses;
using ClipboxGuard.Services.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboxGuard.Services
{
    public class MainProcessKeeperService : IMainProcessKeeperService, IDisposable
    {
        private const int MAIN_PROCESS_EXIT_TIMEOUT_MS = 3000;
        private const int QUIT_PIPE_MSG_TIMEOUT_MS = 3000;
        
        private readonly ISearchProcessKeeperService _searchProcessKeeperService;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly string _installationPath;
        private readonly int _sessionId;
        private readonly NamedPipesClient _pipeClient;
        private readonly NamedPipesServer _pipeServer;
        private readonly MainProcessPinger _mainProcessPinger;

        private Process _mainProcess;
        private bool _isMainProcessExitRequested;
        private bool _isDisposed;

        public event EventHandler<RestartFatalErrorEventArgs> FatalErrorOccurred;
        
        public MainProcessKeeperService(ISearchProcessKeeperService searchProcessKeeperService,
            IPersistenceProvider persistenceProvider,
            string installationPath,
            int sessionId)
        {
            _searchProcessKeeperService = searchProcessKeeperService;
            _persistenceProvider = persistenceProvider;
            _installationPath = installationPath;
            _sessionId = sessionId;

            _mainProcessPinger = new MainProcessPinger(sessionId);
            _mainProcessPinger.PingFailed += _mainProcessPinger_PingFailed;

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            _pipeClient = new NamedPipesClient($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");

            _pipeServer = new NamedPipesServer($"{ProductConstants.GUARD_NAMED_PIPE_PREFIX}.{sessionId}");
            _pipeServer.GotMessage += (s, args) =>
            {
                switch (args.ReceivedMessage)
                {
                    // when the guard recieves QUIT command, it should stop the main process as well
                    case ProductConstants.PIPE_COMMAND_QUIT:
                        StopMainProcess();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Application.Current.Shutdown
                                (
                                        (int)ProcessExitCodes.Ok
                                );
                        });
                        break;

                    case ProductConstants.PIPE_COMMAND_START_MAIN_PROCESS:
                        StartMainProcess(stopFirstIfRunning: false);
                        break;
                }
            };

            _searchProcessKeeperService.RestartOfMainProcessRequested += _searchProcessKeeperService_RestartOfMainProcessRequested;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    _mainProcessPinger.Stop();
                    break;
                case SessionSwitchReason.SessionUnlock:
                    _mainProcessPinger.Start();
                    break;
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                _mainProcessPinger.Stop();
            }
            else if (e.Mode == PowerModes.Resume)
            {
                _mainProcessPinger.Start();
            }
        }

        private void _mainProcessPinger_PingFailed(object sender, EventArgs e)
        {
            StartMainProcess(stopFirstIfRunning: true);
        }

        public void StartExternalProcessManagement()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MainProcessKeeperService));
            }

            StartMainProcess(stopFirstIfRunning: true);
        }

        private void _searchProcessKeeperService_RestartOfMainProcessRequested(object sender, EventArgs e)
        {
            if (CheckIsMainProcessRunning())
            {
                throw new InvalidOperationException("The main process should have stopped by itself by now. It is still running. It is not planned to stop it from the guard.");
            }

            StartMainProcess(stopFirstIfRunning: false);
        }

        public void RestartMainProcess()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MainProcessKeeperService));
            }

            StartMainProcess(stopFirstIfRunning: true);
        }

        private void StartMainProcess(bool stopFirstIfRunning)
        {
            _isMainProcessExitRequested = false;

            string mainProcessPath = Path.Combine(_installationPath, ProductConstants.MAIN_PROCESS_EXE_NAME);
            if (!File.Exists(mainProcessPath))
            {
                throw new FileNotFoundException(mainProcessPath);
            }

            if (CheckIsMainProcessRunning())
            {
                if (stopFirstIfRunning)
                {
                    StopMainProcess();
                }
                else
                {
                    throw new InvalidOperationException($"Main process is already started in the current session. (session id: {_sessionId})");
                }
            }

            _mainProcessPinger.Stop();

            bool runWithHighPriority = _persistenceProvider
                .CheckIfKeyExists(ProductConstants.RUN_WITH_HIGH_PRIORITY_FLAG_KEY);
            
            _mainProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mainProcessPath,
                    Arguments = ProductConstants.STARTED_FROM_GUARD_ARG,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            _mainProcess.Exited += _mainProcess_Exited;
            _mainProcess.Start();
            _mainProcess.EnableRaisingEvents = true;
            _mainProcess.PriorityClass = runWithHighPriority ?
                    ProcessPriorityClass.High
                    : ProcessPriorityClass.Normal;

            _mainProcessPinger.Start();
        }

        private void _mainProcess_Exited(object sender, EventArgs e)
        {
            if (_isMainProcessExitRequested)
            {
                return;
            }

            _mainProcess.Exited -= _mainProcess_Exited;
            
            int exitCode = _mainProcess.ExitCode;
            string stdErr = _mainProcess.StandardError.ReadToEnd();

            _mainProcessPinger.Stop();
            _mainProcess.Dispose();

            if (exitCode == (int)ProcessExitCodes.Ok)
            {
                try
                {
                    HideDeskBand();
                }
                catch { }

                // the user wants the app to quit, so we
                // need to shut the guard down as well.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown
                      (
                               (int)ProcessExitCodes.Ok
                      );
                });
            }
            else
            {
                try { _persistenceProvider.Write(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY, stdErr); }
                catch { }

                try
                {
                    ReleaseKeys();
                }
                catch { }

                var args = new RestartFatalErrorEventArgs(stdErr);
                FatalErrorOccurred?.Invoke(this, args);

                if (args.RestartProcess)
                {
                    StartMainProcess(stopFirstIfRunning: false);
                }
                else
                {
                    try
                    {
                        HideDeskBand();
                    }
                    catch { }
                }
            }
        }

        private bool CheckIsMainProcessRunning()
        {
            IEnumerable<int> pids = GetMainProcessPids();
            return pids.Any();
        }

        private IEnumerable<int> GetMainProcessPids()
        {
            string mainProcessPath = Path.Combine(_installationPath, ProductConstants.MAIN_PROCESS_EXE_NAME);
            IEnumerable<int> pids = ProcessUtils.GetProcessIdsByPathAndSessionId(mainProcessPath, _sessionId);
            return pids;
        }

        public void StopMainProcess()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MainProcessKeeperService));
            }

            _isMainProcessExitRequested = true;

            _mainProcessPinger.Stop();

            if (!CheckIsMainProcessRunning())
            {
                return;
            }

            try
            {
                var task = Task.Run(() => _pipeClient.SendMessage(ProductConstants.PIPE_COMMAND_QUIT));
                if (!task.Wait(QUIT_PIPE_MSG_TIMEOUT_MS))
                {
                    throw new TimeoutException();
                }
            }
            catch (Exception ex) when (ex is TimeoutException || ex is IOException)
            {
                if (!CheckIsMainProcessRunning())
                {
                    // the process has stopped in the meantime
                    return;
                }
            }

            try
            {
                if (_mainProcess == null)
                {
                    // if this instance has not started the main process,
                    // this indicates that something has gone wrong and 
                    // there can be more than one main process due to
                    // some previous exception in the guard.
                    // We should kill all these processed.
                    foreach (int pid in GetMainProcessPids())
                    {
                        using (Process mp = Process.GetProcessById(pid))
                        {
                            // they may have received QUIT message from the named pipe call above.
                            mp.WaitForExit(MAIN_PROCESS_EXIT_TIMEOUT_MS);
                            // if they fail to quit in a timely manner, kill them.
                            if (!mp.HasExited)
                            {
                                try
                                {
                                    mp.Kill();
                                }
                                catch (InvalidOperationException)
                                {
                                    // the process has exitted
                                }
                            }
                        }
                    }
                }
                else
                {
                    _mainProcess.WaitForExit(MAIN_PROCESS_EXIT_TIMEOUT_MS);
                    if (!_mainProcess.HasExited)
                    {
                        try
                        {
                            _mainProcess.Kill();
                        }
                        catch(InvalidOperationException)
                        {
                            // the process has exitted
                        }
                    }

                    _mainProcess.Dispose();
                }
            }
            catch (InvalidOperationException)
            {
                // the process has already exitted 
                // (see https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=netcore-3.1)
            }
            catch (Win32Exception)
            {
                // the process is probably still terminating
                Thread.Sleep(MAIN_PROCESS_EXIT_TIMEOUT_MS);
                if (CheckIsMainProcessRunning())
                {
                    throw new InvalidOperationException("Main process could not be stopped.");
                }
            }
            finally
            {
                _mainProcess?.Dispose();
            }

            try
            {
                HideDeskBand();
            }
            catch { }
        }

        private static void HideDeskBand()
        {
            Guid _deskbandGuid = new Guid(ProductConstants.DESKBAND_SHELL_EXT_GUID);

            if (DeskbandUtils.IsTaskBandShown(_deskbandGuid))
            {
                DeskbandUtils.HideDeskband(_deskbandGuid);
            }
        }

        private static void ReleaseKeys()
        {
            // Release the ctrl key, if it is pressed
            if (KeyboardUtils.IsKeyPressed(Clipbox.Win32.Native.NativeEnums.Keys.LeftControl))
            {
                KeyboardUtils.SimulateKeyUp(Clipbox.Win32.Native.NativeEnums.Keys.LeftControl);
            }
            else if (KeyboardUtils.IsKeyPressed(Clipbox.Win32.Native.NativeEnums.Keys.RightContol))
            {
                KeyboardUtils.SimulateKeyUp(Clipbox.Win32.Native.NativeEnums.Keys.RightContol);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                try
                {
                    StopMainProcess();

                    // request stopping of the ping thread to stop
                    // and wait for it to stop
                    _mainProcess.Dispose();
                }
                catch { }
                finally
                {
                    _isDisposed = true;
                }
            }
        }



        private class MainProcessPinger : IDisposable
        {
            private const int PING_INTERVAL_MS = 5000;
            private const int PING_TIMEOUT_MS = 10000;

            private readonly ManualResetEvent _stopMre;
            private readonly SynchronizationContext _mainSyncContext;
            private readonly NamedPipesClient _pipeClient;

            private bool _isPingThreadEnabled;
            private bool _isDisposed;

            public event EventHandler PingFailed;
            public MainProcessPinger(int sessionId)
            {
                _pipeClient = new NamedPipesClient($"{ProductConstants.MAINPROC_GUARD_COMM_NAMED_PIPE_PREFIX}.{sessionId}");
                _stopMre = new ManualResetEvent(true);
                _mainSyncContext = SynchronizationContext.Current;
            }

            public void Start()
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MainProcessPinger));
                }

                Task.Run(() =>
                {
                    try
                    {
                        MainProcessContinuousPing();
                    }
                    catch (Exception ex)
                    {
                        var edi = ExceptionDispatchInfo.Capture(ex);
                        _mainSyncContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                    }
                });
            }

            public void Stop()
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(MainProcessPinger));
                }

                _isPingThreadEnabled = false;
                _stopMre.WaitOne();
            }


            private void MainProcessContinuousPing()
            {
                _isPingThreadEnabled = true;
                _stopMre.Reset();

                while (_isPingThreadEnabled)
                {
                    try
                    {
                        var task = Task.Run(() =>
                        { 
                            try 
                            { 
                                _pipeClient.SendMessage(ProductConstants.PIPE_COMMAND_PING); 
                            } 
                            catch 
                            {
                                if (_isPingThreadEnabled)
                                {
                                    // stop pinging after an exception
                                    _isPingThreadEnabled = false;
                                    RaisePingFailed();
                                }
                            }
                        });

                        if (!task.Wait(PING_TIMEOUT_MS))
                        {
                            throw new TimeoutException();
                        }

                        Thread.Sleep(PING_INTERVAL_MS);
                    }
                    catch
                    {
                        if (_isPingThreadEnabled)
                        {
                            // stop pinging after an exception
                            _isPingThreadEnabled = false;
                            RaisePingFailed();
                        }
                    }
                }

                _stopMre.Set();
            }

            private void RaisePingFailed()
            {
                // every exception here means that something went wrong with the main process
                Task.Run(() =>
                {
                    try
                    {
                        PingFailed?.Invoke(this, new EventArgs());
                    }
                    catch (Exception ex)
                    {
                        var edi = ExceptionDispatchInfo.Capture(ex);
                        _mainSyncContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                    }
                });
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Stop();
                    _stopMre.Dispose();
                    _isDisposed = true;
                }
            }
        }
    }
}
