using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Clipbox.Win32
{
    public static class SessionUtils
    {
        private static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        private static List<NativeStructures.WTS_SESSION_INFO> GetSessions()
        {
            List<NativeStructures.WTS_SESSION_INFO> sessions = new List<NativeStructures.WTS_SESSION_INFO>();

            IntPtr sessionInfoPtr = IntPtr.Zero;

            int count = 0;
            int returnedValue = WtsApi32.WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref sessionInfoPtr, ref count);
            int dataSize = Marshal.SizeOf(typeof(NativeStructures.WTS_SESSION_INFO));

            Int64 current = IntPtr.Size == 4 ?
                sessionInfoPtr.ToInt32()
                : sessionInfoPtr.ToInt64(); // we will never run on a system with more than 2^32-1 sessions, so the cast is safe...

            if (returnedValue != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    NativeStructures.WTS_SESSION_INFO si = (NativeStructures.WTS_SESSION_INFO)Marshal.PtrToStructure(new IntPtr(current), typeof(NativeStructures.WTS_SESSION_INFO));
                    current += dataSize;

                    sessions.Add(si);
                }

                WtsApi32.WTSFreeMemory(sessionInfoPtr);
            }

            return sessions;
        }

        public static IEnumerable<int> GetActiveSessionIds()
        {
            return GetSessions().
                Where(si => si.State == NativeEnums.WTS_CONNECTSTATE_CLASS.WTSActive).
                Select(si => si.SessionID);
        }

        private static int _sessionId = -1;
        public static int GetCurrentSessionId()
        {
            if (_sessionId == -1)
            {
                using (Process myProcess = Process.GetCurrentProcess())
                {
                    _sessionId = myProcess.SessionId;
                }
            }

            return _sessionId;
        }

        private static string _sid;
        /// <summary>
        /// Returns the current sid of the user if the function succeeds,
        /// or an empty string otherwise.
        /// </summary>
        /// <returns></returns>
        public static string GetUserSid()
        {
            if (_sid == null)
            {
                try
                {
                    _sid = WindowsIdentity.GetCurrent()?.User?.Value
                            ?? UserPrincipal.Current?.Sid?.Value
                            ?? string.Empty;
                }
                catch
                {
                    _sid = string.Empty;
                }
            }

            return _sid;
        }

        /// <summary>
        /// Tries to construct a unique session id. This is not the logon sid, but a
        /// custom id instead and there is no guarantee that it will be stable for 
        /// the processess with different privileges. However, we are interested in
        /// our current process which , so this doesn't matter.
        /// </summary>
        /// <returns></returns>
        public static string GetSessionUniqueId()
        {
            int sessionId = GetCurrentSessionId();
            string sid = GetUserSid();
            if (sessionId > -1 && !string.IsNullOrEmpty(sid))
            {
                return $"{sessionId}:{sid}";
            }
            else
            {
                return string.Empty;
            }
        }

        
    }
}
