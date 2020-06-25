using System.Runtime.InteropServices;

namespace Clipbox.Win32.Native
{
    public static class PsApi
    {
        [DllImport(ExternDll.PsApi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out NativeStructures.PerformanceInformation performanceInformation, [In] int size);

    }
}
