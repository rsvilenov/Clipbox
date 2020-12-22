using System;
using System.Runtime.InteropServices;
using System.Text;
using static Clipbox.Win32.Native.NativeEnums;

namespace Clipbox.Win32.Native
{
    public static class Kernel32
    {
        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GlobalSize(IntPtr handle);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern uint GetLastError();

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport(ExternDll.Kernel32, EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport(ExternDll.Kernel32)]
        public static extern unsafe void CopyMemory(void* dst, void* src, uint length);
        
        [DllImport(ExternDll.Kernel32, EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Gets the real system thread id (safe for STA threads only)
        /// </summary>
        /// <returns></returns>
        [DllImport(ExternDll.Kernel32)]
        public static extern uint GetCurrentThreadId();

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool QueryFullProcessImageName(IntPtr hprocess, int dwFlags,
                       StringBuilder lpExeName, out int size);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess,
                       bool bInheritHandle, int dwProcessId);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        internal static extern bool GetDevicePowerState(IntPtr hDevice, out bool pfOn);
    }
}
