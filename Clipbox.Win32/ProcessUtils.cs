using Clipbox.Win32.Native;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace Clipbox.Win32
{
    public static class ProcessUtils
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [Flags]
        private enum CreateProcessFlags : uint
        {
            DEBUG_PROCESS = 0x00000001,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            CREATE_SUSPENDED = 0x00000004,
            DETACHED_PROCESS = 0x00000008,
            CREATE_NEW_CONSOLE = 0x00000010,
            NORMAL_PRIORITY_CLASS = 0x00000020,
            IDLE_PRIORITY_CLASS = 0x00000040,
            HIGH_PRIORITY_CLASS = 0x00000080,
            REALTIME_PRIORITY_CLASS = 0x00000100,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_FORCEDOS = 0x00002000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x00004000,
            ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000,
            INHERIT_PARENT_AFFINITY = 0x00010000,
            INHERIT_CALLER_PRIORITY = 0x00020000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000,
            PROCESS_MODE_BACKGROUND_END = 0x00200000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NO_WINDOW = 0x08000000,
            PROFILE_USER = 0x10000000,
            PROFILE_KERNEL = 0x20000000,
            PROFILE_SERVER = 0x40000000,
            CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000,
        }

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private extern static bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandle,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("userenv.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, int bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        private static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        private static extern bool
           DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
                            ref SECURITY_ATTRIBUTES lpThreadAttributes,
                            Int32 ImpersonationLevel, Int32 dwTokenType,
                            ref IntPtr phNewToken);

        public const int GENERIC_ALL_ACCESS = 0x10000000;

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("shell32.dll")]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);
        

        public static void SetProcessTaskManagerGroupName(string appId)
        {
            // check for Windows 7
            Version version = Environment.OSVersion.Version;
            if ((version.Major > 6) || (version.Major == 6 && version.Minor >= 1))
            {
                int result = SetCurrentProcessExplicitAppUserModelID(appId);
                if (result != 0)
                {
                    throw new Win32Exception();
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Windows 7 or later is required.");
            }
        }

        public static uint Spawn(string[] parameters, bool createNewProcessGroup = false, bool startHighPriority = false)
        {
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            IntPtr hToken = WindowsIdentity.GetCurrent().Token;
            IntPtr hDupedToken = IntPtr.Zero;

            bool result = DuplicateTokenEx(
                      hToken,
                      GENERIC_ALL_ACCESS,
                      ref sa,
                      (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                      (int)TOKEN_TYPE.TokenPrimary,
                      ref hDupedToken
                   );

            if (!result)
            {
                throw new Win32Exception();
            }


            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";

            CreateProcessFlags dwCreationFlags = CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT;

            if (startHighPriority)
            {
                dwCreationFlags |= CreateProcessFlags.HIGH_PRIORITY_CLASS;
            }
            if (createNewProcessGroup)
            {
                dwCreationFlags |= CreateProcessFlags.CREATE_NEW_PROCESS_GROUP;
            }

            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;// new Uri(applicationPath).LocalPath;
            string commandLine = $"{applicationPath} {string.Join(" ", parameters)}";

            var procInfo = new PROCESS_INFORMATION();
            result = false;
            try
            {
                // create a new process in the current user's logon session
                result = CreateProcessAsUser(hToken,        // client's access token
                                                null,                   // file to execute
                                                commandLine,        // command line
                                                ref sa,                 // pointer to process SECURITY_ATTRIBUTES
                                                ref sa,                 // pointer to thread SECURITY_ATTRIBUTES
                                                false,                  // handles are not inheritable
                                                (uint)dwCreationFlags,        // creation flags
                                                IntPtr.Zero,            // pointer to new environment block 
                                                null,                   // name of current directory 
                                                ref si,                 // pointer to STARTUPINFO structure
                                                out procInfo            // receives information about new process
                                                );

                if (!result)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                CloseHandle(procInfo.hProcess);
                CloseHandle(procInfo.hThread);
            }

            return procInfo.dwProcessId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hToken"></param>
        /// <param name="applicationPath"></param>
        /// <param name="startHighPriority"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception">The process creation failed</exception>  
        public static uint StartProcessWithToken(IntPtr hToken, string applicationPath, bool startHighPriority)
        {
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";
            
            CreateProcessFlags dwCreationFlags = CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT;

            if (startHighPriority)
            {
                dwCreationFlags |= CreateProcessFlags.HIGH_PRIORITY_CLASS;
            }

            if (applicationPath.StartsWith("file:"))
            {
                applicationPath = new Uri(applicationPath).LocalPath;
            }

            IntPtr env = IntPtr.Zero;
            if (!CreateEnvironmentBlock(out env, hToken, 0))
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError, "CreateEnvironmentBlock Error " + lastError);
            }

            var procInfo = new PROCESS_INFORMATION();
            bool result = false;
            try
            {
                // create a new process in the current user's logon session
                result = CreateProcessAsUser(hToken,        // client's access token
                                                null,                   // file to execute
                                                applicationPath,        // command line
                                                ref sa,                 // pointer to process SECURITY_ATTRIBUTES
                                                ref sa,                 // pointer to thread SECURITY_ATTRIBUTES
                                                false,                  // handles are not inheritable
                                                (uint)dwCreationFlags,        // creation flags
                                                env,            // pointer to new environment block 
                                                null,                   // name of current directory 
                                                ref si,                 // pointer to STARTUPINFO structure
                                                out procInfo            // receives information about new process
                                                );

                if (!result)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {

                DestroyEnvironmentBlock(env);
                CloseHandle(procInfo.hProcess);
                CloseHandle(procInfo.hThread);
            }

            return procInfo.dwProcessId;
        }

        /// <summary>
        /// Starts a process in a specified session
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="commandLine"></param>
        /// <param name="startElevated"></param>
        /// <param name="startHighPriority"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception">The process creation failed</exception>  
        public static uint StartProcessWithSessionId(int sessionId, string commandLine, bool startElevated, bool startHighPriority)
        {
            var hToken = TokenUtils.GetUserTokenForSession(sessionId);
            
            IntPtr hElevatedToken = IntPtr.Zero;
            if (startElevated && CheckIsUacEnabled())
            {
                hElevatedToken = TokenUtils.TryGetElevatedToken(hToken);
            }

            uint pid = 0;
            try
            {
                pid = StartProcessWithToken(hElevatedToken != IntPtr.Zero ? hElevatedToken : hToken, commandLine, startHighPriority);
            }
            finally
            {
                if (hElevatedToken != IntPtr.Zero)
                {
                    CloseHandle(hElevatedToken);
                }

                CloseHandle(hToken);
            }

            return pid;
        }
        
        #region Checking

        private const int SESSION_ID_ALL_SESSIONS = -1;

        /// <summary>
        /// Gets all PIDs of processess, having the specified path
        /// </summary>
        /// <param name="processPath">The full path of the process</param>
        /// <param name="sessionId">The session id</param>
        /// <returns></returns>
        public static IEnumerable<int> GetProcessIdsByPathAndSessionId(string processPath, int sessionId)
        {
            if (Path.GetFileName(processPath) == processPath)
            {
                throw new ArgumentException($"Please provide path, not filename: {processPath}");
            }

            var procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processPath));
            List<int> matchingProcs = new List<int>();
            foreach (Process proc in procs)
            {
                try
                {
                    string processFullPath = GetExecutablePath(proc.Id);
                    if (proc.SessionId == sessionId &&
                        processFullPath.Equals(processPath, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingProcs.Add(proc.Id);
                    }
                }
                catch (InvalidOperationException)
                { }
                finally
                {
                    proc.Dispose();
                }

            }

            return matchingProcs;
        }

        /// <summary>
        /// Checks whether a process with a given name exists
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <returns></returns>
        public static bool CheckProcessExists(string processName)
        {
            return CheckProcessExistsInSessionInternal(processName, SESSION_ID_ALL_SESSIONS);
        }

        /// <summary>
        /// Checks whether a process with the given name exists
        /// in the given session
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <param name="sessionId">The session id</param>
        /// <returns></returns>
        public static bool CheckProcessExistsInSession(string processName, int sessionId)
        {
            return CheckProcessExistsInSessionInternal(processName, sessionId);
        }

        private static bool CheckProcessExistsInSessionInternal(string processName, int sessionId)
        {
            var procs = Process.GetProcessesByName(processName);
            foreach (Process proc in procs)
            {
                if (sessionId == SESSION_ID_ALL_SESSIONS)
                {
                    return true;
                }

                try
                {
                    if (proc.SessionId == sessionId)
                    {
                        return true;
                    }
                }
                catch(InvalidOperationException)
                { }
                finally
                {
                    proc.Dispose();
                }
            }
            return false;
        }


        #endregion

        #region Executable image info

        /// NOT WORKING WITH ELEVATED PROCESSES DUE TO OpenProcess HAVING NO ACCESS TO THEM FROM UNELEVATED CALLERS
        /// <summary>
        /// Get the path of the process, regardless of its elevation level.
        /// System.Diagnostics.Process.MainModule.FileName fails to obtain the path of elevated processes,
        /// when called from a non-elevated process. This method is a workaround.
        /// <see href="http://pinvoke.net/default.aspx/kernel32.QueryFullProcessImageName"/>  
        /// <see href="https://github.com/xedoc/JoystickCurves/blob/530239ae6c9542ade388963625bc142afeb0304d/JoystickCurves/ExecutablePath.cs"/> 
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        internal static string GetExecutablePath(int pid)
        {
            var buffer = new StringBuilder(1024);
            IntPtr hprocess = Kernel32.OpenProcess(NativeEnums.ProcessAccessFlags.QueryLimitedInformation,
                                          false, pid);
            if (hprocess != IntPtr.Zero)
            {
                try
                {
                    int size = buffer.Capacity;
                    if (Kernel32.QueryFullProcessImageName(hprocess, 0, buffer, out size))
                    {
                        return buffer.ToString();
                    }
                }
                finally
                {
                    Kernel32.CloseHandle(hprocess);
                }
            }
            throw new Win32Exception();
        }

        #endregion

        private static bool CheckIsUacEnabled()
        {
            const string uacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            const string uacRegistryValue = "EnableLUA";

            using (RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(uacRegistryKey, false))
            {
                return uacKey.GetValue(uacRegistryValue).Equals(1);
            }
        }

        public static bool IsOurProcessRunningElevated
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
