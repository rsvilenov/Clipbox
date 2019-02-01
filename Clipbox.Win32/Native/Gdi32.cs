using System;
using System.Runtime.InteropServices;

namespace Clipbox.Win32.Native
{
    public static class Gdi32
    {
        [DllImport(ExternDll.Gdi32)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport(ExternDll.Gdi32)]
        public static extern uint GetMetaFileBitsEx(IntPtr hmf, uint nSize, IntPtr lpvData);

        [DllImport(ExternDll.Gdi32)]
        public static extern IntPtr SetMetaFileBitsEx(uint bufferSize, byte[] buffer);

        [DllImport(ExternDll.Gdi32)]
        public static extern IntPtr SetWinMetaFileBits(uint bufferSize, byte[] pData, IntPtr hDC, IntPtr lpStruct);

        [DllImport(ExternDll.Gdi32)]
        public static extern bool DeleteMetaFile(IntPtr hmf);

    }
}
