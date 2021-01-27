using Clipbox.ExtendedClipboard.EventArgs;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Services.Interfaces;
using Clipbox.Win32;
using System;
using System.Timers;
using System.Windows.Interop;

namespace Clipbox.Services
{
    /// <summary>
    /// This implementation of IClipboardEventProvider places a window in the clipboard chain.
    /// This method is considered fragile. Sometimes after several days of running,
    /// the window stops receiving WM_DRAWCLIPBOARD messages (I don't know why).
    /// </summary>
    [Obsolete("Use ClipboardFormatListenerService instead.")]
    public class ClipboardListenerService : IClipboardListenerService, IClipboardEventProvider, IDisposable
    {
        const int DRAWCLIPBOARD_WM_PREVENT_REPETITION_DELAY_MS = 20;

        private readonly HwndSource _hWndSource;
        private readonly Timer _msgDelayTimer;

        private IntPtr _nextClipBoardViewerHwnd;
        private bool _isWithinRepetitionInterval;
        private bool _isClipboardViewerAlreadyRemoved;
        private bool _isDisposed;

        public ClipboardListenerService()
        {
            _msgDelayTimer = new Timer(DRAWCLIPBOARD_WM_PREVENT_REPETITION_DELAY_MS);
            _msgDelayTimer.AutoReset = true;
            _msgDelayTimer.Elapsed += (sender, args) => _isWithinRepetitionInterval = false;

            // Create a message-only window. 
            // https://social.msdn.microsoft.com/Forums/en-US/dadf671b-faa3-45f7-9b67-42c3b5959373/messageonly-windows-in-win32device?forum=vssmartdevicesnative
            // According to the link above, it should be sufficient to pass NULL (HWND_DESKTOP) as a parent hWnd.
            _hWndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, IntPtr.Zero /* HWND_DESKTOP */);

            // Hook the window's WndProc
            _hWndSource.AddHook(new HwndSourceHook(WndProc));

            // Place the window in the clipboard chain
            _nextClipBoardViewerHwnd = Win32.Native.User32.SetClipboardViewer(_hWndSource.Handle);

            if (_nextClipBoardViewerHwnd != IntPtr.Zero)
            {
                MarshalExt.ThrowOnWin32Error();
            }
        }

        public event EventHandler<ClipboardEventArgs> ClipboardContentChanged;

        public IntPtr GetEventWindowHandle()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return _hWndSource.Handle;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32.Native.NativeConstants.WmConstants.WM_DRAWCLIPBOARD:

                    // We may receive this message multiple times for a single change
                    // https://stackoverflow.com/questions/18895865/getclipboarddata-on-a-delay-rendered-data-inside-a-clipboard-viewer-causes-recur
                    if (_isWithinRepetitionInterval)
                    {
                        Win32.Native.User32.SendMessage(_nextClipBoardViewerHwnd, msg, wParam, lParam);
                        return IntPtr.Zero;
                    }

                    // do not fire the event for clipboard entries, comming from our own process
                    if (Win32.Native.User32.GetClipboardOwner() == _hWndSource.Handle)
                    {
                        Win32.Native.User32.SendMessage(_nextClipBoardViewerHwnd, msg, wParam, lParam);
                        return IntPtr.Zero;
                    }

                    // pass the message to the next viewer, unless the clipboard change
                    // should be cancelled (e.g. drag without drop)
                    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms649019(v=vs.85).aspx

                    var cbListenerEventArgs = new ClipboardEventArgs();
                    ClipboardContentChanged?.Invoke(hwnd, cbListenerEventArgs);

                    Win32.Native.User32.SendMessage(_nextClipBoardViewerHwnd, msg, wParam, lParam);

                    _isWithinRepetitionInterval = true;
                    _msgDelayTimer.Start();
                    break;

                case Win32.Native.NativeConstants.WmConstants.WM_CHANGECBCHAIN:
                    if (wParam.Equals(_nextClipBoardViewerHwnd))
                    {
                        _nextClipBoardViewerHwnd = lParam;
                    }
                    else if (!_nextClipBoardViewerHwnd.Equals(IntPtr.Zero))
                    {
                        Win32.Native.User32.SendMessage(_nextClipBoardViewerHwnd, msg, wParam, lParam);
                    }
                    break;

                case Win32.Native.NativeConstants.WmConstants.WM_DESTROY:
                    if (!_isClipboardViewerAlreadyRemoved)
                    {
                        Win32.Native.User32.ChangeClipboardChain(hwnd, _nextClipBoardViewerHwnd);
                        // the msdn article on this api doesn't say anything about its
                        // possible error codes, but we will check them nevertheless
                        MarshalExt.ThrowOnWin32Error();

                        _isClipboardViewerAlreadyRemoved = true;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                // remove the window from the clipboard chain
                try
                {
                    if (!_isClipboardViewerAlreadyRemoved)
                    {
                        Win32.Native.User32.ChangeClipboardChain(_hWndSource.Handle, _nextClipBoardViewerHwnd);
                        MarshalExt.ThrowOnWin32Error();
                        _isClipboardViewerAlreadyRemoved = true;
                    }
                }
                finally
                {
                    try { _hWndSource.RemoveHook(WndProc); }
                    finally { _hWndSource.Dispose(); }
                }

                _isDisposed = true;
            }
            
        }
    }
}
