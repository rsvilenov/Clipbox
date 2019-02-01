using System;
using System.Runtime.InteropServices;

namespace Clipbox.Win32.Native
{
    internal static class WtsApi32
    {
        [DllImport(ExternDll.WtsApi32)]
        internal static extern Int32 WTSEnumerateSessions(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] Int32 Reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 Version,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport(ExternDll.WtsApi32)]
        internal static extern void WTSFreeMemory(IntPtr pMemory);
    }
}
