using Clipbox.Win32.Native;
using ClipboxGuard.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace ClipboxGuard.Services
{
    public class ClipboardCheckerService : IClipboardCheckerService, IDisposable
    {
        private const int CHECK_CLIPBOARD_INTERVAL_MS = 5000;
        private const int CHECK_CLIPBOARD_INTERVAL_WHEN_LOCKED_MS = 500;
        private const int NUMBER_OF_FAILED_ATTEMPTS_ALLOWED = 3;

        private readonly ManualResetEvent _clipboardCheckerWorkerStoppingMre;
        private readonly IMainProcessKeeperService _mainProcessKeeperService;
        private readonly HwndSource _hWndSource;
        private readonly SynchronizationContext _mainSyncContext;

        private bool _isCheckingClipboardEnabled;
        private bool _isDisposed;

        public ClipboardCheckerService(IMainProcessKeeperService mainProcessKeeperService)
        {
            _mainProcessKeeperService = mainProcessKeeperService;

            _mainSyncContext = SynchronizationContext.Current;
            _clipboardCheckerWorkerStoppingMre = new ManualResetEvent(true);
            _hWndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, IntPtr.Zero /* HWND_DESKTOP */);

            Task.Run(() => 
            { 
                try
                {
                    CheckClipboard();
                }
                catch (Exception ex)
                {
                    var edi = ExceptionDispatchInfo.Capture(ex);
                    _mainSyncContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                }
            });
        }

        private void CheckClipboard()
        {
            _isCheckingClipboardEnabled = true;

            int numberOfFailedAttempts = 0;
            while (_isCheckingClipboardEnabled)
            {
                bool clipboardOpenSuccess;
                try
                {
                    clipboardOpenSuccess = User32.OpenClipboard(_hWndSource.Handle);
                }
                catch
                {
                    try { User32.CloseClipboard(); } catch { }
                    clipboardOpenSuccess = false;
                }

                if (!clipboardOpenSuccess)
                {
                    if (numberOfFailedAttempts == NUMBER_OF_FAILED_ATTEMPTS_ALLOWED)
                    {
                        numberOfFailedAttempts = 0;
                        _mainProcessKeeperService.RestartMainProcess();
                        Thread.Sleep(CHECK_CLIPBOARD_INTERVAL_MS);
                    }
                    else
                    {
                        numberOfFailedAttempts++;
                        Thread.Sleep(CHECK_CLIPBOARD_INTERVAL_WHEN_LOCKED_MS);
                    }
                }
                else
                {
                    numberOfFailedAttempts = 0;
                    try { User32.CloseClipboard(); } catch { }
                    Thread.Sleep(CHECK_CLIPBOARD_INTERVAL_MS);
                }
            }

            _clipboardCheckerWorkerStoppingMre.Set();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                _isDisposed = true;
                _isCheckingClipboardEnabled = false;
                _clipboardCheckerWorkerStoppingMre.WaitOne();
            }
        }


    }
}
