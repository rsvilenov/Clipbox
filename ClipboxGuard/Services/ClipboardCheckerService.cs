using Clipbox.Win32;
using Clipbox.Win32.Native;
using ClipboxGuard.Services.Interfaces;
using Microsoft.Win32;
using System;
using System.Runtime.ExceptionServices;
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

        private bool _isCheckingClipboardThreadEnabled;
        private bool _isCheckingClipboardEnabled;
        private bool _isDisposed;

        public ClipboardCheckerService(IMainProcessKeeperService mainProcessKeeperService)
        {
            _mainProcessKeeperService = mainProcessKeeperService;

            _mainSyncContext = SynchronizationContext.Current;
            _clipboardCheckerWorkerStoppingMre = new ManualResetEvent(true);
            _hWndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, IntPtr.Zero /* HWND_DESKTOP */);

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

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

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.ConsoleDisconnect:
                case SessionSwitchReason.RemoteDisconnect:
                    _isCheckingClipboardEnabled = false;
                    break;
                case SessionSwitchReason.SessionUnlock:
                case SessionSwitchReason.ConsoleConnect:
                case SessionSwitchReason.RemoteConnect:
                    _isCheckingClipboardEnabled = true;
                    break;
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                _isCheckingClipboardEnabled = false;
            }
            else if (e.Mode == PowerModes.Resume)
            {
                _isCheckingClipboardEnabled = true;
            }
        }

        private void CheckClipboard()
        {
            _isCheckingClipboardThreadEnabled = true;
            _isCheckingClipboardEnabled = true;
            int numberOfFailedAttempts = 0;

            while (_isCheckingClipboardThreadEnabled)
            {
                if (!_isCheckingClipboardEnabled)
                {
                    Thread.Sleep(CHECK_CLIPBOARD_INTERVAL_MS);
                    continue;
                }

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
                    if (GetClipboardOwnerPid() == _mainProcessKeeperService.MainProcessPid)
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
                        Thread.Sleep(CHECK_CLIPBOARD_INTERVAL_MS);
                        continue;
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

        private int GetClipboardOwnerPid()
        {
            int pid = -1;

            try
            {
                IntPtr clipboardOwnerHwnd = User32.GetClipboardOwner();

                if (clipboardOwnerHwnd != IntPtr.Zero)
                {
                    pid = WindowUtils.GetPidByHWnd(clipboardOwnerHwnd);
                }
            }
            catch { }

            return pid;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                _isDisposed = true;
                _isCheckingClipboardThreadEnabled = false;
                _clipboardCheckerWorkerStoppingMre.WaitOne();
            }
        }
    }
}
