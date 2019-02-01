using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

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

            int current = sessionInfoPtr.ToInt32();

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

        public static int GetCurrentSessionId()
        {
            using (Process myProcess = Process.GetCurrentProcess())
            {
                return myProcess.SessionId;
            }
        }
    }
}
