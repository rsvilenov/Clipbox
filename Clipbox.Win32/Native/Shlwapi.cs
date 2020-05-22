using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Clipbox.Win32.Native
{
    public static class ShlwApi
    {
        [DllImport(ExternDll.ShlwApi, BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        internal static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, uint cchOutBuf, IntPtr ppvReserved);
    }
}
