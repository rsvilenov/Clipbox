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
    /// This implementation of IClipboardEventProvider uses the recommended way for
    /// receiving clipboard change notifications - subscribing to WM_CLIPBOARDUPDATE.
    /// </summary>
    class ClipboardFormatListenerService : IClipboardListenerService, IClipboardEventProvider, IDisposable
    {
        const int CLIPBOARDUPDATE_WM_PREVENT_REPETITION_DELAY_MS = 20;

        private readonly HwndSource _hWndSource;
        private readonly Timer _msgDelayTimer;

        private bool _isWithinRepetitionInterval;
        private bool _isClipboardViewerAlreadyRemoved;
        private bool _isDisposed;

        public ClipboardFormatListenerService()
        {
            _msgDelayTimer = new Timer(CLIPBOARDUPDATE_WM_PREVENT_REPETITION_DELAY_MS);
            _msgDelayTimer.AutoReset = true;
            _msgDelayTimer.Elapsed += (sender, args) => _isWithinRepetitionInterval = false;

            // Create a message-only window. 
            // https://social.msdn.microsoft.com/Forums/en-US/dadf671b-faa3-45f7-9b67-42c3b5959373/messageonly-windows-in-win32device?forum=vssmartdevicesnative
            // According to the link above, it should be sufficient to pass NULL (HWND_DESKTOP) as a parent hWnd.
            _hWndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, IntPtr.Zero /* HWND_DESKTOP */);

            // Hook the window's WndProc
            _hWndSource.AddHook(new HwndSourceHook(WndProc));

            if (!Win32.Native.User32.AddClipboardFormatListener(_hWndSource.Handle))
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
                case Win32.Native.NativeConstants.WmConstants.WM_CLIPBOARDUPDATE:

                    // We may receive this message multiple times for a single change
                    // https://stackoverflow.com/questions/18895865/getclipboarddata-on-a-delay-rendered-data-inside-a-clipboard-viewer-causes-recur
                    if (_isWithinRepetitionInterval)
                    {
                        return IntPtr.Zero;
                    }

                    // do not fire the event for clipboard entries, comming from our own process
                    if (Win32.Native.User32.GetClipboardOwner() == _hWndSource.Handle)
                    {
                        return IntPtr.Zero;
                    }

                    // pass the message to the next viewer, unless the clipboard change
                    // should be cancelled (e.g. drag without drop)
                    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms649019(v=vs.85).aspx

                    var cbListenerEventArgs = new ClipboardEventArgs();
                    ClipboardContentChanged?.Invoke(hwnd, cbListenerEventArgs);

                    _isWithinRepetitionInterval = true;
                    _msgDelayTimer.Start();
                    break;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                try
                {
                    if (!_isClipboardViewerAlreadyRemoved)
                    {
                        Win32.Native.User32.RemoveClipboardFormatListener(_hWndSource.Handle);
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
