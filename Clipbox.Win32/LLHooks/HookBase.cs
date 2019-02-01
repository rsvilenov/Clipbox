using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace Clipbox.Win32.LLHooks
{
    public abstract class HookBase : IDisposable
    {
        private IntPtr _hookID = IntPtr.Zero;

        private bool _isHookRunning = false;

        private User32.LowLevelHookProc _hookProc;

        private Thread _hookThread;

        private ManualResetEvent _hookThreadExitEvent;
        
        protected abstract CallbackResult OnCallback(int nCode, IntPtr wParam, IntPtr lParam);

        protected abstract NativeEnums.HookType TypeOfHook
        {
            get;
        }

        protected HookBase()
        {
            _hookThreadExitEvent = new ManualResetEvent(false);

            // save a reference to the callback method so that it won't get GCd
            _hookProc = HookCallback;
        }

        public void Start()
        {
            if (_isHookRunning)
                return;
            
            // We run the hook in its own thread in order to avoid blocking the mouse/keyboard when
            // our consumer threads perform long running tasks. We also do not want to get unhooked by
            // Windows for holding the hooked operation too long.
            _hookThread = new Thread(() =>
            {
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/9bb5e76d-e9a3-4264-a9e9-842e6ff5ac32/setwindowshookex-works-in-net-2-but-not-in-net-4?forum=vbgeneral
                // thank you noorbakhsh
                IntPtr hMod = IntPtr.Zero; //Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]); /*Kernel32.GetModuleHandle(curModule.ModuleName) */

                _hookID = User32.SetWindowsHookEx((int)TypeOfHook, _hookProc, hMod, 0);
                if (_hookID == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                _isHookRunning = true;

                _hookThreadExitEvent.Reset();
                _hookThreadExitEvent.WaitOne();

                if (!User32.UnhookWindowsHookEx(_hookID))
                {
                    throw new Win32Exception();
                }

                _isHookRunning = false;
            });

            // Set the appartment state to STA in order to make sure that the CRL never changes the underlying system thread.
            // We want to make sure that by the time we call UnhookWindowsHookEx, the underlying system thread has remained the same.
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();
        }
        
        public void Stop()
        {
            _hookThreadExitEvent.Set();
        }
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            CallbackResult callbackResult = OnCallback(nCode, wParam, lParam);

            if (!callbackResult.IsDefaultResult)
            {
                return callbackResult.PtrResult;
            }

            return User32.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_isHookRunning)
            {
                Stop();
            }
        }

        protected class CallbackResult
        {
            public IntPtr PtrResult { get; private set; }

            public bool IsDefaultResult { get; private set; }

            private CallbackResult(IntPtr ptrResult, bool isDefaultResult = false)
            {
                PtrResult = ptrResult;
                IsDefaultResult = isDefaultResult;
            }
            
            public static CallbackResult DefaultResult
            {
                get
                {
                    return new CallbackResult(IntPtr.Zero, true);
                }
            }

            public static CallbackResult FromIntPtr(IntPtr ptr)
            {
                return new CallbackResult(ptr);
            }
        }
    }
}
