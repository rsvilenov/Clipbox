using System;
using System.Runtime.InteropServices;

namespace Clipbox.Win32.Native
{
    public static class Shell32
    {
        [DllImport(ExternDll.Shell32, SetLastError = true)]
        public static extern void SHChangeNotify(NativeEnums.SHCNE wEventId, NativeEnums.SHChangeNotifyFlags uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport(ExternDll.Shell32, SetLastError = true)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref NativeStructures.SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    }
}
