using System.Runtime.InteropServices;

namespace Clipbox.Win32.Native
{
    public static class Wer
    {
        [DllImport(ExternDll.Wer, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int WerAddExcludedApplication(string pwzExeName, bool bAllUsers);
    }
}
