using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Clipbox.Win32
{
    public static class SessionUtils2
    {

        public static IEnumerable<int> GetActiveSessionIds()
        {
            Process[] processes = Process.GetProcessesByName("winlogon");
            //Process[] processes = Process.GetProcessesByName("explorer");
            foreach (Process p in processes)
            {
                yield return p.SessionId;
            }
        }

        /// <summary>
        /// Class that allows running applications with full admin rights. In
        /// addition the application launched will bypass the Vista UAC prompt.
        /// </summary>
        public class ApplicationLoader
        {
            #region Structures

            public enum LogonFlags
            {
                WithProfile = 1,
                NetCredentialsOnly
            }

            public enum CreationFlags
            {
                DefaultErrorMode = 0x04000000,
                NewConsole = 0x00000010,
                NewProcessGroup = 0x00000200,
                SeparateWOWVDM = 0x00000800,
                Suspended = 0x00000004,
                UnicodeEnvironment = 0x00000400,
                ExtendedStartupInfoPresent = 0x00080000
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_ATTRIBUTES
            {
                public int Length;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct STARTUPINFO
            {
                public int cb;
                public String lpReserved;
                public String lpDesktop;
                public String lpTitle;
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
            public struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public uint dwProcessId;
                public uint dwThreadId;
            }

            #endregion

            #region Enumerations

            enum TOKEN_TYPE : int
            {
                TokenPrimary = 1,
                TokenImpersonation = 2
            }

            enum SECURITY_IMPERSONATION_LEVEL : int
            {
                SecurityAnonymous = 0,
                SecurityIdentification = 1,
                SecurityImpersonation = 2,
                SecurityDelegation = 3,
            }

            #endregion

            #region Constants

            //public const int TOKEN_DUPLICATE = 0x0002;
            public const uint MAXIMUM_ALLOWED = 0x2000000;
            public const int CREATE_NEW_CONSOLE = 0x00000010;
            public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;


            public const int IDLE_PRIORITY_CLASS = 0x40;
            public const int NORMAL_PRIORITY_CLASS = 0x20;
            public const int HIGH_PRIORITY_CLASS = 0x80;
            public const int REALTIME_PRIORITY_CLASS = 0x100;



            public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
            public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
            public const UInt32 TOKEN_DUPLICATE = 0x0002;
            public const UInt32 TOKEN_IMPERSONATE = 0x0004;
            public const UInt32 TOKEN_QUERY = 0x0008;
            public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
            public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
            public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
            public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
            public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
            public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
            public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                TOKEN_ADJUST_SESSIONID);

            #endregion

            #region Win32 API Imports

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool CloseHandle(IntPtr hSnapshot);

            [DllImport("kernel32.dll")]
            static extern uint WTSGetActiveConsoleSessionId();

            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcessWithTokenW(
                IntPtr hToken,
                LogonFlags dwLogonFlags,
                string lpApplicationName,
                string lpCommandLine,
                CreationFlags dwCreationFlags,
                IntPtr lpEnvironment,
                string lpCurrentDirectory,
                [In] ref STARTUPINFO lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
            public extern static bool CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
                ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
                String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
            //        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            //        static extern bool CreateProcessAsUserW(
            //IntPtr hToken,
            //string lpApplicationName,
            //string lpCommandLine,
            //ref SECURITY_ATTRIBUTES lpProcessAttributes,
            //ref SECURITY_ATTRIBUTES lpThreadAttributes,
            //bool bInheritHandles,
            //uint dwCreationFlags,
            //IntPtr lpEnvironment,
            //string lpCurrentDirectory,
            //ref STARTUPINFO lpStartupInfo,
            //out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll")]
            static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

            [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
            public extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
                ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType,
                int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

            [DllImport("kernel32.dll")]
            static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("advapi32.dll", SetLastError = true), SuppressUnmanagedCodeSecurity]
            static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

            [DllImport("userenv.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment,
IntPtr hToken, int bInherit);

            [DllImport("userenv.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
            #endregion

            [Flags]
            enum CreateProcessFlags
            {
                CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
                CREATE_DEFAULT_ERROR_MODE = 0x04000000,
                CREATE_NEW_CONSOLE = 0x00000010,
                CREATE_NEW_PROCESS_GROUP = 0x00000200,
                CREATE_NO_WINDOW = 0x08000000,
                CREATE_PROTECTED_PROCESS = 0x00040000,
                CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
                CREATE_SEPARATE_WOW_VDM = 0x00000800,
                CREATE_SHARED_WOW_VDM = 0x00001000,
                CREATE_SUSPENDED = 0x00000004,
                CREATE_UNICODE_ENVIRONMENT = 0x00000400,
                DEBUG_ONLY_THIS_PROCESS = 0x00000002,
                DEBUG_PROCESS = 0x00000001,
                DETACHED_PROCESS = 0x00000008,
                EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
                INHERIT_PARENT_AFFINITY = 0x00010000
            }

            // https://stackoverflow.com/questions/25726172/launch-process-from-session-0-isolation
            /// <summary>
            /// Launches the given application with full admin rights, and in addition bypasses the Vista UAC prompt
            /// </summary>
            /// <param name="applicationPath">The name of the application to launch</param>
            /// <param name="procInfo">Process information regarding the launched application that gets returned to the caller</param>
            /// <returns></returns>
            public static bool StartProcessAndBypassUAC(String applicationPath, string startingDir, int sessionId, out PROCESS_INFORMATION procInfo)
            {
                Console.WriteLine("StartProcessAndBypassUAC started.");
                uint winlogonPid = 0;
                IntPtr hUserTokenDup = IntPtr.Zero, hPToken = IntPtr.Zero, hProcess = IntPtr.Zero;
                procInfo = new PROCESS_INFORMATION();

                // obtain the currently active session id; every logged on user in the system has a unique session id
                uint dwSessionId = (uint)sessionId;//WTSGetActiveConsoleSessionId();

                // obtain the process id of the winlogon process that is running within the currently active session
                // -- chaged by ty 
                //Process[] processes = Process.GetProcessesByName("taskhostw");
                Process[] processes = Process.GetProcessesByName("explorer");
                foreach (Process p in processes)
                {
                    if ((uint)p.SessionId == dwSessionId)
                    {
                        winlogonPid = (uint)p.Id;
                    }
                }

                // obtain a handle to the winlogon process
                hProcess = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);

                // obtain a handle to the access token of the winlogon process
                if (!OpenProcessToken(hProcess, (int)TOKEN_DUPLICATE, ref hPToken))
                {
                    Console.WriteLine("Last error OpenProcessToken: " + Marshal.GetLastWin32Error());

                    CloseHandle(hProcess);
                    return false;
                }

                // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
                // I would prefer to not have to use a security attribute variable and to just 
                // simply pass null and inherit (by default) the security attributes
                // of the existing token. However, in C# structures are value types and therefore
                // cannot be assigned the null value.
                SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                sa.Length = Marshal.SizeOf(sa);

                // copy the access token of the winlogon process; the newly created token will be a primary token
                //if (!DuplicateTokenEx(hPToken, TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
                if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
                {
                    Console.WriteLine("Last error DuplicateTokenEx: " + Marshal.GetLastWin32Error());
                    CloseHandle(hProcess);
                    CloseHandle(hPToken);
                    return false;
                }

                // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
                // the window station has a desktop that is invisible and the process is incapable of receiving
                // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
                // interaction with the new process.
                STARTUPINFO si = new STARTUPINFO();
                si.cb = (int)Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default"; // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

                // flags that specify the priority and creation method of the process
                int dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT;

                if (startingDir.StartsWith("file:"))
                {
                    startingDir = new Uri(startingDir).LocalPath;
                }

                if (applicationPath.StartsWith("file:"))
                {
                    applicationPath = new Uri(applicationPath).LocalPath;
                }

                IntPtr env = IntPtr.Zero;
                if (!CreateEnvironmentBlock(out env, hUserTokenDup, 0))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(lastError, "CreateEnvironmentBlock Error " + lastError);
                }

                bool result = false;
                try
                {
                    //// create a new process in the current user's logon session
                    result = CreateProcessAsUser(hUserTokenDup,        // client's access token
                                                    null,                   // file to execute
                                                    applicationPath,        // command line
                                                    ref sa,                 // pointer to process SECURITY_ATTRIBUTES
                                                    ref sa,                 // pointer to thread SECURITY_ATTRIBUTES
                                                    false,                  // handles are not inheritable
                                                    dwCreationFlags,        // creation flags
                                                    env,            // pointer to new environment block 
                                                    startingDir,                   // name of current directory 
                                                    ref si,                 // pointer to STARTUPINFO structure
                                                    out procInfo            // receives information about new process
                                                    );

                    //Console.WriteLine(applicationPath);
                    //Console.WriteLine(startingDir);


                    //result = CreateProcessWithTokenW(hUserTokenDup, LogonFlags.WithProfile,
                    //    applicationPath, applicationPath,
                    //    CreationFlags.DefaultErrorMode | CreationFlags.UnicodeEnvironment | CreationFlags.NewConsole, env,
                    //    startingDir, ref si, out procInfo);


                    if (!result)
                    {
                        Console.WriteLine("Last error: " + Marshal.GetLastWin32Error());
                        throw new Win32Exception("Env: " + Marshal.PtrToStringUni(env) + "Last error: " + Marshal.GetLastWin32Error() + new Win32Exception(Marshal.GetLastWin32Error()).Message);
                    }
                    else
                    {
                        Console.WriteLine("Process ID: " + procInfo.dwProcessId);
                    }
                }
                finally
                {

                    DestroyEnvironmentBlock(env);

                }


                // invalidate the handles
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                CloseHandle(hUserTokenDup);

                return result; // return the result
            }

        }
    }
}

