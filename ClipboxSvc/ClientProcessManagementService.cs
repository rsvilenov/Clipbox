using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.IPC;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace ClipboxSvc
{
    public partial class ClientProcessManagementService : ServiceBase
    {
        private Dictionary<int, int> _pidSessionIdMapping = new Dictionary<int, int>();

        private bool _stopping;

        private NamedPipesServer _namedPipesServer;

        public ClientProcessManagementService()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
        }

        public void OnDebug()
        {
            OnStart(null);
            Console.ReadKey();
            //System.Threading.Thread.Sleep(20000);
            //OnStop();
        }

        protected override void OnStart(string[] args)
        {
            foreach (int sessionId in SessionUtils.GetActiveSessionIds().Where(s => s != 0))
            {
                int pid = StartClientProcess(sessionId);
                _pidSessionIdMapping.Add(pid, sessionId);
            }

            const int serviceSessionId = 0;

            if (SessionUtils.GetCurrentSessionId() != serviceSessionId)
            {
                throw new InvalidOperationException("This service must be started under session 0.");
            }

            _namedPipesServer = new NamedPipesServer($"{ProductConstants.SERVICE_GUARD_NAMED_PIPE_PREFIX}.{serviceSessionId}");
            _namedPipesServer.GotMessage += _namedPipesServer_GotMessage;
        }

        private void _namedPipesServer_GotMessage(object sender, NamedPipesResponseEventArgs e)
        {
            int sessionId;
            if (int.TryParse(e.ReceivedMessage, out sessionId))
            {
                int pid = StartClientProcess(sessionId);
                _pidSessionIdMapping.Add(pid, sessionId);
            }
        }

        protected override void OnStop()
        {
            _stopping = true;

            foreach (int sessionId in SessionUtils.GetActiveSessionIds().Where(s => s != 0))
            {
                try { KillClientProcess(sessionId); } catch { }
            }

            _namedPipesServer.Dispose();
        }

        private void KillClientProcess(int sessionId)
        {
            IEnumerable<int> allProcIds = ProcessUtils.GetProcessIdsByPathAndSessionId(Path.Combine(InstallationPath, ProductConstants.CLIENT_EXE_NAME), sessionId);

            if (allProcIds.Any())
            {
                using (Process clientProcessForSession = Process.GetProcessById(allProcIds.First()))
                {
                    if (!clientProcessForSession.HasExited)
                    {
                        clientProcessForSession.Kill();
                    }
                }
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            if (changeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                int pid = StartClientProcess(changeDescription.SessionId);
                _pidSessionIdMapping.Add(pid, changeDescription.SessionId);
            }
            
            base.OnSessionChange(changeDescription);
        }
        

        private int StartClientProcess(int sessionId)
        {
            string fullClientExePath = Path.Combine(InstallationPath, ProductConstants.CLIENT_EXE_NAME);
            if (!File.Exists(fullClientExePath ))
            {
                throw new FileNotFoundException(fullClientExePath);
            }

#if RELEASE
            // TODO: this leaves us vulnerable
            //AssemblyUtils.AssertManagedAssemblySecurity(fullClientExePath);
            AssemblyUtils.AssertAssemblyHashCode(fullClientExePath, ProductConstants.CLIENT_EXE_HASH.ToArray());
#endif

            string fullStartupCmd = $"{fullClientExePath} {ProductConstants.STARTED_FROM_SERVICE_ARG}";

            uint clientPid = ProcessUtils.StartProcessWithSessionId(
                                        sessionId,
                                        fullStartupCmd,  
                                        startElevated: true,
                                        startHighPriority: true);

            Process process = Process.GetProcessById((int)clientPid);
            process.EnableRaisingEvents = true;
            process.Exited += ClientProcess_Exited;
            return (int)clientPid;
        }

        private void ClientProcess_Exited(object sender, System.EventArgs e)
        {
            using (Process process = (Process)sender)
            {
                // the exit code of a process, terminated by Process.Kill() is less than,
                // or equal to 0.
                // https://stackoverflow.com/questions/2455232/exit-code-of-a-process-terminated-with-process-kill-in-c-sharp
                if (process.ExitCode != (int)ProcessExitCodes.Ok && !_stopping)
                {
                    // restart the process
                    int sessionId = _pidSessionIdMapping[process.Id];
                    _pidSessionIdMapping.Remove(process.Id);

                    StartGuardProcess(sessionId);
                }

                process.Exited -= ClientProcess_Exited;
            }
        }

        private void StartGuardProcess(int sessionId)
        {
            string fullGuardExePath = Path.Combine(InstallationPath, ProductConstants.GUARD_EXE_NAME);

            string fullStartupCmd = $"{fullGuardExePath} {ProductConstants.STARTED_FROM_SERVICE_ARG}";

            uint guardPid = ProcessUtils.StartProcessWithSessionId(
                                            sessionId,
                                            fullStartupCmd,
                                            startElevated: false,
                                            startHighPriority: false);

            _pidSessionIdMapping.Add((int)guardPid, sessionId);

            Process guardProcess = Process.GetProcessById((int)guardPid);
            guardProcess.EnableRaisingEvents = true;
            guardProcess.Exited += GuardProcess_Exited;
        }

        private void GuardProcess_Exited(object sender, EventArgs e)
        {
            using (Process guardProcess = (Process)sender)
            {
                if (guardProcess.ExitCode == (int)ProcessExitCodes.Special)
                {
                    // restart the client process
                    int sessionId = _pidSessionIdMapping[guardProcess.Id];
                    _pidSessionIdMapping.Remove(guardProcess.Id);

                    int clientPid = StartClientProcess(sessionId);
                    _pidSessionIdMapping.Add(clientPid, sessionId);
                }

                guardProcess.Exited -= GuardProcess_Exited;
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
