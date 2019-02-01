using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace ClipboxGuard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Guid _deskbandGuid = new Guid("3C6D5961-E6C7-44F6-A522-A20DE33D2DAC");
        
        private const string lastExceptionLogFile = "lastException.log";

        private NamedPipesServer _namedPipeServer;

        private int _mySessionId;

        private bool _stopping;

        private bool _isStartedFromService = false;
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            string[] startupArgs = Environment.GetCommandLineArgs();
            _mySessionId = SessionUtils.GetCurrentSessionId();

            Application.Current.Dispatcher.ShutdownStarted += (s, args)
                => _stopping = true;

            if (startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SERVICE_ARG))
            {
                _isStartedFromService = true;
                ReleaseKeys();
                HideDeskBand();
                ShowErrorWindow();
            }
            else if (startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SCHEDULER_ARG))
            {
                StartClientProcess();

                _namedPipeServer = new NamedPipesServer($"{ProductConstants.SERVICE_GUARD_NAMED_PIPE_PREFIX}.{_mySessionId}");
                _namedPipeServer.GotMessage += _namedPipeServer_GotMessage;
            }
            else
            {
                MessageBox.Show("This executable is not meant to be started by the user.");
                Shutdown((int)ProcessExitCodes.Ok);
            }
        }
        

        private void _namedPipeServer_GotMessage(object sender, NamedPipesResponseEventArgs e)
        {
            int callerProcessId;
            if (int.TryParse(e.Response, out callerProcessId))
            {
                var pids = ProcessUtils.GetProcessIdsByPathAndSessionId(Path.Combine(InstallationPath, ProductConstants.CLIENT_EXE_NAME), _mySessionId);
                bool hasProcsWithTheSameName = pids.Any(pid => pid != callerProcessId);

                if (!hasProcsWithTheSameName)
                {
                    StartClientProcess();
                }
            }
        }

        private void StartClientProcess()
        {
            string installationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            installationPath = new Uri(installationPath).LocalPath;
            string fullClientExePath = Path.Combine(installationPath, ProductConstants.CLIENT_EXE_NAME);
            if (!File.Exists(fullClientExePath))
            {
                throw new FileNotFoundException(fullClientExePath);
            }

#if RELEASE
            //AssemblyUtils.AssertManagedAssemblySecurity(fullClientExePath);
            AssemblyUtils.AssertAssemblyHashCode(fullClientExePath, ProductConstants.CLIENT_EXE_HASH.ToArray());
#endif

            ProcessStartInfo psi = new ProcessStartInfo(fullClientExePath, ProductConstants.STARTED_FROM_GUARD_ARG);
            Process clientProcess = Process.Start(psi);
            clientProcess.PriorityClass = ProcessPriorityClass.High;
            clientProcess.EnableRaisingEvents = true;
            clientProcess.Exited += ClientProcess_Exited;
        }

        private void ClientProcess_Exited(object sender, EventArgs e)
        {
            using (Process process = (Process)sender)
            {
                // the exit code of a process, terminated by Process.Kill() is less than,
                // or equal to 0.
                // https://stackoverflow.com/questions/2455232/exit-code-of-a-process-terminated-with-process-kill-in-c-sharp
                if (process.ExitCode != (int)ProcessExitCodes.Ok && !_stopping)
                {
                    ShowErrorWindow();
                }

                process.Exited -= ClientProcess_Exited;
            }
        }

        private void ShowErrorWindow()
        {
            string lastException = AppDataUtils.ReadTextFromApplicationData(lastExceptionLogFile);

            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)(() =>
            {
                var errorWindow = new ErrorWindow(lastException);
                errorWindow.Closed += ErrorWinow_Closed;
            }));
        }

        private static void HideDeskBand()
        {
            if (DeskbandUtils.IsTaskBandShown(_deskbandGuid))
            {
                DeskbandUtils.HideDeskband(_deskbandGuid);
            }
        }

        private static void ReleaseKeys()
        {
            // Release the ctrl key, if it is pressed
            if (Clipbox.Win32.KeyboardUtils.IsKeyPressed(Clipbox.Win32.Native.NativeEnums.Keys.LeftControl))
            {
                Clipbox.Win32.KeyboardUtils.SimulateKeyUp(Clipbox.Win32.Native.NativeEnums.Keys.LeftControl);
            }
            else if (Clipbox.Win32.KeyboardUtils.IsKeyPressed(Clipbox.Win32.Native.NativeEnums.Keys.RightContol))
            {
                Clipbox.Win32.KeyboardUtils.SimulateKeyUp(Clipbox.Win32.Native.NativeEnums.Keys.RightContol);
            }
        }

        private void ErrorWinow_Closed(object sender, EventArgs e)
        {
            AppDataUtils.WriteToApplicationData(lastExceptionLogFile, null, append: false);

            var errorWindow = (ErrorWindow)sender;
            if (_isStartedFromService)
            {
                Shutdown
                    (
                        // if the 'restart' checkbox is checked,
                        // tell the service that it should start the client process
                        // by exitting with 'Special' exit code
                        errorWindow.IsRestartClientProcessCbChecked ?
                            (int)ProcessExitCodes.Special :
                            (int)ProcessExitCodes.Ok
                    );
            }
            else
            {
                if (errorWindow.IsRestartClientProcessCbChecked)
                {
                    StartClientProcess();
                }
            }
        }

        private string InstallationPath
        {
            get
            {
                return new Uri
                            (
                                Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase)
                            ).LocalPath;
            }
        }
    }
}
