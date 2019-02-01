using Clipbox.Core;
using Clipbox.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Clipbox.Infrastructure.IPC
{
    // TODO: break this into client and sever classes
    public class WindowMessageIPC : DisposableObject
    {
        private HwndSource _hWndSource;
        private HwndSourceHook _hWndSourceHook;

        private uint _wmCustomIpc;
        private IntPtr _remoteHwnd;
        private int _remotePid;
        private int _localPid;

        private const string WM_CUSTOMIPC = "WM_CUSTOMIPC";
        public IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        const int ERROR_TIMEOUT = 1460;
        const int ERROR_INVALID_HWND = 1400;


        public event EventHandler RemoteListenerReady;

        public event EventHandler QuitRequestReceived;


        public class StringEventArgs : EventArgs
        {
            public string Value { get; set; }
        }

        public event EventHandler<StringEventArgs> StringReceived;

        public IntPtr MessageWindowHwnd
        {
            get
            {
                return _hWndSource.Handle;
            }
        }

        /// <summary>
        /// Instantiates WindowMessageIPC in server mode
        /// </summary>
        public WindowMessageIPC()
        {
            _wmCustomIpc = Win32.Native.User32.RegisterWindowMessage(WM_CUSTOMIPC);
            if (_wmCustomIpc == 0)
            {
                MarshalExt.ThrowOnWin32Error();
            }

            _hWndSource = new HwndSource(0, 0, 0, 0, 0, null, IntPtr.Zero /* HWND_DESKTOP */);
            _hWndSourceHook = new HwndSourceHook(WndProc);
            _hWndSource.AddHook(_hWndSourceHook);

            using (Process p = Process.GetCurrentProcess())
            {
                _localPid = p.Id;
                Win32.Native.User32.PostMessage(HWND_BROADCAST, _wmCustomIpc, _hWndSource.Handle, new IntPtr(p.Id));
            }
        }

        /// <summary>
        /// Instantiates WindowMessageIPC in client mode
        /// </summary>
        public WindowMessageIPC(int remotePid) : this()
        {
            if (!IsProcessRunning(remotePid))
            {
                throw new InvalidOperationException($"There is no process with id {remotePid}.");
            }

            _remotePid = remotePid;
        }
        
        public bool PingServer(int timeout)
        {
            UIntPtr lpdwResult;
            IntPtr result = Win32.Native.User32.SendMessageTimeout(_remoteHwnd, Win32.Native.NativeConstants.WmConstants.WM_NULL, IntPtr.Zero, IntPtr.Zero, Win32.Native.NativeEnums.SendMessageTimeoutFlags.SMTO_NORMAL, (uint)timeout, out lpdwResult);
            if (result == IntPtr.Zero)
            {
                int lastWin32Error = Marshal.GetLastWin32Error();
                if (lastWin32Error == ERROR_TIMEOUT ||  lastWin32Error == ERROR_INVALID_HWND)
                {
                    return false;
                }
                else
                {
                    throw new Win32Exception(lastWin32Error);
                }
            }

            return true;
        }
        
        public void PostQuitRequest()
        {
            if (!IsProcessRunning(_remotePid))
            {
                throw new InvalidOperationException($"The process with id {_remotePid} has ended.");
            }

            Win32.Native.User32.PostMessage(_remoteHwnd, Win32.Native.NativeConstants.WmConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        public bool SendString(string value, int timeout = 1000)
        {
            // https://boycook.wordpress.com/2008/07/29/c-win32-messaging-with-sendmessage-and-wm_copydata/

            Win32.Native.NativeStructures.COPYDATASTRUCT cdStruct;
            cdStruct.cbData = value.Length + 1;
            cdStruct.lpData = value;
            cdStruct.dwData = (IntPtr)100;
            
            IntPtr structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cdStruct));
            Marshal.StructureToPtr(cdStruct, structPtr, false);

            UIntPtr result;
            try
            {
                IntPtr ret = Win32.Native.User32.SendMessageTimeout(_remoteHwnd, Win32.Native.NativeConstants.WmConstants.WM_COPYDATA, IntPtr.Zero, structPtr, Win32.Native.NativeEnums.SendMessageTimeoutFlags.SMTO_NORMAL, (uint)timeout, out result);
                if (ret == IntPtr.Zero)
                {
                    int lastWin32Error = Marshal.GetLastWin32Error();
                    if (lastWin32Error == ERROR_TIMEOUT)
                    {
                        return false;
                    }
                    else
                    {
                        throw new Win32Exception(lastWin32Error);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(structPtr);
            }

            return true;
        }

        private bool IsProcessRunning(int pid)
        {
            Process[] processlist = Process.GetProcesses();
            return processlist.Any(pr => pr.Id == pid);
        }
        
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _wmCustomIpc)
            {
                if (lParam.ToInt32() == _localPid)
                {
                    // we have received our own broadcasted message
                    return IntPtr.Zero;
                }
                else
                if (lParam.ToInt32() == _remotePid)
                {
                    // the remote process have answered
                    _remoteHwnd = wParam;
                    RemoteListenerReady?.Invoke(this, new EventArgs());
                }
                else
                {
                    // we have been asked to send our hwnd back
                    Win32.Native.User32.PostMessage(_remoteHwnd, _wmCustomIpc, _hWndSource.Handle, new IntPtr(_localPid));
                }
            }
            else if (msg == Win32.Native.NativeConstants.WmConstants.WM_COPYDATA)
            {
                Win32.Native.NativeStructures.COPYDATASTRUCT cds = Marshal.PtrToStructure<Win32.Native.NativeStructures.COPYDATASTRUCT>(lParam);
                StringReceived?.Invoke(this, new StringEventArgs { Value = cds.lpData });
            }
            else if (msg == Win32.Native.NativeConstants.WmConstants.WM_CLOSE)
            {
                QuitRequestReceived?.Invoke(this, new EventArgs());
            }
            // TODO: implement WM_COPYDATA

            return IntPtr.Zero;
        }

        protected override void Dispose(bool disposing)
        {
            _hWndSource.Dispatcher.Invoke(() =>
            {
                _hWndSource.RemoveHook(_hWndSourceHook);
                _hWndSource.Dispose();
            });
        }
    }
}
