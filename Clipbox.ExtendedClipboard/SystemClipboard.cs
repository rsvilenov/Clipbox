using Clipbox.ExtendedClipboard.OleInftastructure;
using Clipbox.Infrastructure.Threading;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;

namespace Clipbox.ExtendedClipboard
{
    public class SystemClipboard : IDisposable
    {
        const int OPEN_CLIPBOARD_ATTEMPTS = 5;

        const int OPEN_CLIPBOARD_ATTEMPT_INTERVAL_MS = 300;

        /// <summary>
        /// This is the error code that the clipboard api returns when
        /// the clipboard has not been opened. This sometimes happens regardless
        /// of whether the OpenClipboard call has been performed (TODO: find out why). 
        /// </summary>
        const int ERROR_CLIPBOARD_NOT_OPEN = 1418;
        
        const int CLIPBOARD_OPERATION_ATTEMPTS = 5;


        private OleDispatcher _oleDispatcher;

        private SimpleDispatcher _clipboardDispatcher;

        private static object _openClipboardLockHandle = new object();
        
        private bool _isClipboardLockedOpen;

        public SystemClipboard()
        {
            _clipboardDispatcher = new SimpleDispatcher();
            _oleDispatcher = new OleDispatcher(_clipboardDispatcher);
        }

        public Dictionary<uint, string> GetAvailableFormats()
        {
            return DoClipboardOperation(() => EnumerateClipboardFormats());
        }

        public void TryLockClipboardOpen()
        {
            _clipboardDispatcher.Invoke(() =>
            {
                try
                {
                    TryOpenClipboard();
                    _isClipboardLockedOpen = true;
                }
                catch
                {
                    _isClipboardLockedOpen = false;
                    throw;
                }
            });
            
        }

        public void TryCloseAndUnlockClipboard()
        {
            _clipboardDispatcher.Invoke(() =>
            {
                TryCloseClipboard();
                _isClipboardLockedOpen = false;
            });
        }

        public void SetClipboardData(IntPtr dataPtr, string formatName)
        {
            uint format = 0;
            NativeEnums.CLIPFORMAT predefinedFormat;

            if (Enum.TryParse(formatName, out predefinedFormat))
            {
                format = (uint)predefinedFormat;
            }
            else
            {
                // when the format is already registered, RegisterClipboardFormat returns its number
                format = User32.RegisterClipboardFormat(formatName);
            }

            if (format == 0)
            {
                throw new Win32Exception();
            }

            SetClipboardData(dataPtr, format);
        }

        public void SetClipboardData(IntPtr dataPtr, uint format)
        {
            DoClipboardOperation<object>(() =>
            {
                bool success = User32.SetClipboardData(format, dataPtr);
                if (!success)
                {
                    throw new Win32Exception();
                }
                return null;
            });
        }

        public IntPtr GetClipboardData(uint format)
        {
            return DoClipboardOperation(() =>
            {
                IntPtr handle = User32.GetClipboardData(format);
                if (handle == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                return handle;
            });
        }

        public IntPtr GetClipboardLastUserHWnd()
        {
            return User32.GetClipboardOwner();
        }

        public void EmptyClipboard()
        {
            DoClipboardOperation<object>(() =>
            {
                bool success = User32.EmptyClipboard();
                if (!success)
                {
                    throw new Win32Exception();
                }
                return null;
            });
        }

        public IDataObject OleGetClipboard()
        {
            IDataObject dataObject = null;
            _oleDispatcher.InvokeOLE((ole) => ole.OleGetClipboard(ref dataObject));
            return dataObject;
        }

        public void OleSetClipboard(IDataObject dataObject)
        {
            _oleDispatcher.InvokeOLE((ole) => ole.OleSetClipboard(dataObject));
        }

        public void Dispose()
        {
            try { _oleDispatcher.Dispose(); }
            finally { _clipboardDispatcher.Dispose(); }
        }

        private Dictionary<uint, string> EnumerateClipboardFormats()
        {
            var formats = new Dictionary<uint, string>();

            uint lastRetrievedFormat = 0;
            while (0 != (lastRetrievedFormat = User32.EnumClipboardFormats(lastRetrievedFormat)))
            {
                if (!formats.ContainsKey(lastRetrievedFormat))
                {
                    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms649040(v=vs.85).aspx
                    // In the docs for GetClipboardFormatName it is said for the format parameter that 
                    // "The type of format to be retrieved. This parameter must not specify any of the predefined clipboard formats."
                    // Therefore we need to check the format first and if it is predefined, not call GetClipboardFormatName
                    if (Enum.IsDefined(typeof(NativeEnums.CLIPFORMAT), lastRetrievedFormat))
                    {
                        NativeEnums.CLIPFORMAT predefinedFormat = (NativeEnums.CLIPFORMAT)lastRetrievedFormat;
                        formats.Add(lastRetrievedFormat, predefinedFormat.ToString());
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(1000);

                        int result = User32.GetClipboardFormatName(lastRetrievedFormat, sb, sb.Capacity);
                        if (result == 0)
                        {
                            throw new Win32Exception();
                        }

                        formats.Add(lastRetrievedFormat, sb.ToString());
                    }
                }
            }

            // if EnumClipboardFormats returned 0 at the very beginning, then there may be an error
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (lastRetrievedFormat == 0 && lastWin32Error != 0) // 0 = ERROR_SUCCESS. 
            {
                throw new Win32Exception(lastWin32Error);
            }

            return formats;
        }

        private T DoClipboardOperation<T>(Func<T> operation)
        {
            T result = default(T);
            
            // the clipboard cannot be open twice
            lock (_openClipboardLockHandle)
            {
                _clipboardDispatcher.Invoke(() =>
                {
                    if (!_isClipboardLockedOpen)
                    {
                        TryOpenClipboard();
                    }

                    try
                    {
                        for (int i = 0; i < CLIPBOARD_OPERATION_ATTEMPTS; i++)
                        {
                            try
                            {
                                result = operation();
                                break;
                            }
                            catch (Win32Exception wex)
                            {
                                if (wex.NativeErrorCode == ERROR_CLIPBOARD_NOT_OPEN)
                                {
                                    TryCloseClipboard();
                                    TryOpenClipboard();
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (!_isClipboardLockedOpen)
                        {
                            TryCloseClipboard();
                        }
                    }
                });
            }

            return result;
        }

        private static void TryCloseClipboard()
        {
            // TODO: maybe check status and retry
            User32.CloseClipboard();
        }

        private static void TryOpenClipboard()
        {
            bool success = false;
            for (int attempt = 0; attempt < OPEN_CLIPBOARD_ATTEMPTS; attempt++)
            {
                using (var currentProcess = System.Diagnostics.Process.GetCurrentProcess())
                {
                    success = User32.OpenClipboard(currentProcess.MainWindowHandle);
                }

                if (!success)
                {
                    Thread.Sleep(OPEN_CLIPBOARD_ATTEMPT_INTERVAL_MS);
                }
                else
                {
                    break;
                }
            }

            if (!success)
            {
                throw new Win32Exception();
            }
        }
    }
}
