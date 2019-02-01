using System;
using System.Runtime.InteropServices;
using System.Security;

using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Clipbox.Win32.Native
{
    public static class Ole32
    {
        ///<SecurityNote>
        ///  Critical as this code performs an elevation.
        ///</SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleGetClipboard(ref IComDataObject data);

        ///<SecurityNote>
        ///  Critical as this code performs an elevation.
        ///</SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleSetClipboard(IComDataObject pDataObj);

        ///<SecurityNote>
        ///  Critical as this code performs an elevation.
        ///</SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleFlushClipboard();

        ///<SecurityNote>
        ///  Critical: This code calls into unmanaged code which elevates
        ///</SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Ole32, ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int OleUninitialize();

        ///<SecurityNote>
        ///  Critical as this code performs an elevation.
        ///</SecurityNote>
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.Ole32)]
        public static extern int OleInitialize(IntPtr val);
    }
}
