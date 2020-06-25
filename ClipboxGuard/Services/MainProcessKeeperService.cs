using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
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

namespace ClipboxGuard.Services
{
    public class MainProcessKeeperService : IMainProcessKeeperService, IDisposable
    {
        private const int MAIN_PROCESS_EXIT_TIMEOUT_MS = 3000;

        private readonly ISearchProcessKeeperService _searchProcessKeeperService;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly string _installationPath;
        private readonly int _sessionId;
        private readonly NamedPipesClient _pipeClient;
        
        private Process _mainProcess;

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

            _pipeClient = new NamedPipesClient($"{ProductConstants.GUARD_NAMED_PIPE_PREFIX}.{sessionId}");
            
            _searchProcessKeeperService.RestartOfMainProcessRequested += _searchProcessKeeperService_RestartOfMainProcessRequested;
        }

        public void StartExternalProcessManagement()
        {
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

        private void StartMainProcess(bool stopFirstIfRunning)
        {
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
        }

        private void _mainProcess_Exited(object sender, EventArgs e)
        {
            _mainProcess.Exited -= _mainProcess_Exited;
            
            int exitCode = _mainProcess.ExitCode;
            string stdErr = _mainProcess.StandardError.ReadToEnd();

            try { _persistenceProvider.Write(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY, stdErr); }
            catch { }

            _mainProcess.Dispose();

            if (exitCode != (int)ProcessExitCodes.Ok)
            {
                try
                {
                    ReleaseKeys();
                    HideDeskBand();
                }
                catch { }

                var args = new RestartFatalErrorEventArgs(stdErr);
                FatalErrorOccurred?.Invoke(this, args);

                if (args.RestartProcess)
                {
                    StartMainProcess(stopFirstIfRunning: false);
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
            if (!CheckIsMainProcessRunning())
            {
                return;
            }

            try
            {
                _pipeClient.SendMessage(ProductConstants.PIPE_QUIT_COMMAND);
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
                    // there can be more than one main process due to
                    // some previous exception in the guard. Kill them all.
                    foreach (int pid in GetMainProcessPids())
                    {
                        using (Process mp = Process.GetProcessById(pid))
                        {
                            // they may have received QUIT message from the named pipe call above.
                            mp.WaitForExit(MAIN_PROCESS_EXIT_TIMEOUT_MS);
                            // if they fail to quit in a timely manner, kill them.
                            mp.Kill();
                        }
                    }
                }
                else
                {
                    _mainProcess.WaitForExit(MAIN_PROCESS_EXIT_TIMEOUT_MS);
                    _mainProcess.Kill();

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

                _mainProcess?.Dispose();
            }
        }

        private static void HideDeskBand()
        {
            Guid _deskbandGuid = new Guid("3C6D5961-E6C7-44F6-A522-A20DE33D2DAC");

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
            try
            {
                StopMainProcess();
            }
            catch { }
        }
    }
}
