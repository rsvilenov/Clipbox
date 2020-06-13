using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Clipbox.Win32
{
    public static class MarshalExt
    {
        
        const int GMEM_MOVABLE = 0x0002;
        const int GMEM_ZEROINIT = 0x0040;
        const int GHND = GMEM_MOVABLE | GMEM_ZEROINIT;

        /// <summary>
        /// Copies the string as unicode to a memory block, allocated with
        /// GlobalAlloc using GMEM_MOVABLE (required by SetClipboardData)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IntPtr StringToHGlobalMovableUni(string str)
        {
            return StringToHGlobalMovable(str, true);
        }

        /// <summary>
        /// Copies the string as ansi to a memory block, allocated with
        /// GlobalAlloc using GMEM_MOVABLE (required by SetClipboardData)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IntPtr StringToHGlobalMovableAnsi(string str)
        {
            return StringToHGlobalMovable(str, false);
        }

        public static IntPtr HMemToHGlobalMemMovable(IntPtr hMem, uint length)
        {
            var hGlobal = Kernel32.GlobalAlloc(GHND, (UIntPtr)length);
            if (hGlobal == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            var target = Kernel32.GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                Kernel32.CopyMemory(target, hMem, length);
            }
            finally
            {
                Kernel32.GlobalUnlock(target);
            }

            return target;
        }

        public static string HGlobalMemMovableToString(IntPtr hGlobalMovable)
        {
            string result = null;

            int size = Kernel32.GlobalSize(hGlobalMovable);
            IntPtr source = Kernel32.GlobalLock(hGlobalMovable);
            if (source == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                byte[] buffer = new byte[size];
                Marshal.Copy(source, buffer, 0, size);
                result = (new UnicodeEncoding()).GetString(buffer, 0, buffer.Length);
            }
            finally
            {
                Kernel32.GlobalUnlock(source);
            }

            return result;
        }

        public static int GetGlobalSize(IntPtr hGlobalMovable)
        {
            return Kernel32.GlobalSize(hGlobalMovable);
        }
        
        public static IntPtr HGlobalMemMovableToHMem(IntPtr hGlobalMovable, int? globalSize = null)
        {
            int size = globalSize ?? Kernel32.GlobalSize(hGlobalMovable);
            IntPtr source = Kernel32.GlobalLock(hGlobalMovable);
            if (source == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                IntPtr hMem = Marshal.AllocHGlobal(size);
                Kernel32.CopyMemory(hMem, source, (uint)size);
            }
            finally
            {
                Kernel32.GlobalUnlock(source);
            }

            return source;
        }

        private static IntPtr StringToHGlobalMovable(string str, bool useUnicode = false)
        {
            // Windows' default utf-16 uses 2 bytes per char
            int sizeOfChar = useUnicode ? 2 : 1;

            uint bytes = (uint)((str.Length + 1) * sizeOfChar);

            IntPtr source, target;

            if (useUnicode)
            {
                source = Marshal.StringToHGlobalUni(str);
            }
            else
            {
                source = Marshal.StringToHGlobalAnsi(str);
            }

            try
            {
                target = HMemToHGlobalMemMovable(source, bytes);
            }
            finally
            {
                Marshal.FreeHGlobal(source);
            }

            return target;
        }
        
        public static IntPtr CreateHGlobalMovable(int hGlobalSize, Action<IntPtr> action)
        {
            // TODO: this needs refactoring...

            var hGlobal = Kernel32.GlobalAlloc(GHND, (UIntPtr)hGlobalSize);
            if (hGlobal == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            List<Exception> exceptions = new List<Exception>();

            try
            {
                var target = Kernel32.GlobalLock(hGlobal);
                if (target == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                try
                {
                    action(target);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                finally
                {
                    bool unlocked = Kernel32.GlobalUnlock(hGlobal);
                    if (!unlocked)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error != 0) // 0 = NO_ERROR
                        {
                            exceptions.Add(new Win32Exception(error));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                exceptions.Add(ex);
                if (Kernel32.GlobalFree(hGlobal) != IntPtr.Zero)
                {
                    exceptions.Add(new Win32Exception());
                }
            }

            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }
            else if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }

            return hGlobal;
        }

        /// <summary>
        /// Throws a Win32Exception with the last win32 error.
        /// If the error code, returned by GetLastError, is 0 (ERROR_SUCCESS), 
        /// the method returns without throwing.
        /// </summary>
        public static void ThrowOnWin32Error()
        {
            int error = Marshal.GetLastWin32Error();
            if (error != 0 /* ERROR_SUCCESS */)
            {
                throw new Win32Exception(error);
            }
        }
    }
}
