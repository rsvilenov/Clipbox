using Clipbox.Infrastructure.Threading;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.Windows
{
    public class GdiCanvasWindow : IDisposable
    {
        // based on https://stackoverflow.com/questions/128561/registering-a-custom-win32-window-class-from-c-sharp

        private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

        private bool _disposed;
        
        private User32.WndProc _wndProcDelegate;

        private Task _windowWorkerTask;

        private ManualResetEvent _handleMre;

        private ManualResetEvent _contentPaintedMre;

        public IntPtr Handle { get; private set; }

        public bool IsCreationReady { get; private set; }

        public event EventHandler Created;

        public GdiCanvasWindow()
        {
            string className = nameof(GdiCanvasWindow);
            _wndProcDelegate = CustomWndProc;
            IntPtr hInstance = System.Diagnostics.Process.GetCurrentProcess().Handle;

            // Create WNDCLASS
            User32.WNDCLASS wndClass = new User32.WNDCLASS();
            wndClass.style = (uint)NativeEnums.ClassStyles.CS_HREDRAW | (uint)NativeEnums.ClassStyles.CS_VREDRAW | (uint)NativeEnums.ClassStyles.CS_OWNDC;
            wndClass.lpszClassName = className;
            wndClass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            wndClass.hInstance = hInstance;
            wndClass.hCursor = User32.LoadCursor(IntPtr.Zero, (int)NativeConstants.IDC_Constants.IDC_ARROW);

            UInt16 classAtom = User32.RegisterClassW(ref wndClass);

            int lastError = Marshal.GetLastWin32Error();

            if (classAtom == 0 && lastError != ERROR_CLASS_ALREADY_EXISTS)
            {
                throw new Exception("Could not register window class");
            }

            _handleMre = new ManualResetEvent(false);
            _contentPaintedMre = new ManualResetEvent(false);

            // create the window in its own thread so we make sure that all calls to that window that require to 
            // be called in the same thread (e.g. DestroyWindow) are indeed called in this thread, 
            // plus do not rely on the calling thread having a mesage pump running...
            _windowWorkerTask = StaUtils.StartStaTask(() =>
            {
                // Create window
                if (User32.CreateWindowExW(
                0,
                className,
                string.Empty,
                0,
                NativeConstants.CwConstants.CW_USEDEFAULT,
                NativeConstants.CwConstants.CW_USEDEFAULT,
                NativeConstants.CwConstants.CW_USEDEFAULT,
                NativeConstants.CwConstants.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
                ) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                //User32.ShowWindow(Handle, NativeEnums.ShowWindowCommands.ShowMinNoActive);
                //User32.UpdateWindow(Handle);

                NativeStructures.MSG msg;
                int getMessageResult = 0;
                while ((getMessageResult = (User32.GetMessage(out msg, IntPtr.Zero, 0, 0))) != 0)
                {
                    if (getMessageResult == -1)
                    {
                        break;
                    }
                    else
                    {
                        User32.TranslateMessage(ref msg);
                        IntPtr wndProcResult = User32.DispatchMessage(ref msg);
                        // after we have processed WM_CLOSE in WndProc (we have called DestroyWindow)
                        // we need to unregister our class, so that WndProc does not get called anymore.
                        if (msg.message == NativeConstants.WmConstants.WM_CLOSE && wndProcResult == IntPtr.Zero)
                        {
                            if (!User32.UnregisterClass(className, hInstance))
                            {
                                throw new Win32Exception();
                            }
                            break;
                        }
                    }
                }

                Handle = IntPtr.Zero;

            });

        }

        private void CleanUp()
        {
            
        }

        public void Maximize()
        {
            User32.ShowWindow(Handle, NativeEnums.ShowWindowCommands.Maximize);
        }

        public void Hide()
        {
            User32.ShowWindow(Handle, NativeEnums.ShowWindowCommands.Hide);
        }

        public void Show()
        {
            User32.ShowWindow(Handle, NativeEnums.ShowWindowCommands.Show);
        }

        public void Close()
        {
            if (Handle != IntPtr.Zero)
            {
                User32.PostMessage(Handle, NativeConstants.WmConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }


        ~GdiCanvasWindow()
        {
            DisposeInternal(false);
        }

        public void Dispose()
        {
            DisposeInternal(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
        }

        public void WaitHandle()
        {
            _handleMre.WaitOne();
        }

        public void WaitContentPainted()
        {
            _contentPaintedMre.WaitOne();
        }

        private void DisposeInternal(bool disposing)
        {
            try
            {
                Dispose(disposing);
            }
            finally
            {

                if (!_disposed)
                {
                    Close();

                    if (Dispatcher.FromThread(Thread.CurrentThread) != null)
                    {
                        // wait for the sync task to complete without blocking the dispatcher thread
                        while (!_windowWorkerTask.IsCompleted)
                        {
                            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                        }
                    }
                    else
                    {
                        _windowWorkerTask.Wait();
                    }

                    _disposed = true;
                }
            }
        }

        
        protected virtual void OnPainting(Graphics g)
        {
        }

        protected virtual void OnCreateing()
        {
        }

        protected virtual void OnSizeOrPositionChanging(ref System.Drawing.Point position, ref System.Drawing.Size size)
        {
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == NativeConstants.WmConstants.WM_CREATE)
            {
                Handle = hWnd;
                OnCreateing();
                Created?.Invoke(this, new EventArgs());
                IsCreationReady = true;
                _handleMre.Set();
            }
            else if (msg == NativeConstants.WmConstants.WM_PAINT)
            {
                using (Graphics g = Graphics.FromHwnd(hWnd))
                {
                    OnPainting(g);
                }

                _contentPaintedMre.Set();
            }
            else
            // https://blogs.msdn.microsoft.com/llobo/2006/08/01/maximizing-window-with-windowstylenone-considering-taskbar/
            if (msg == NativeConstants.WmConstants.WM_GETMINMAXINFO)
            {
                var rectangle = WindowUtils.GetWindowRectangle(hWnd);
                Point position = new Point(rectangle.Top, rectangle.Left);
                Size size = new Size(rectangle.Size.Width, rectangle.Size.Height);
                OnSizeOrPositionChanging(ref position, ref size);

                NativeStructures.MINMAXINFO mmi = (NativeStructures.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(NativeStructures.MINMAXINFO));
                mmi.ptMaxPosition.X = position.X;
                mmi.ptMaxPosition.Y = position.Y;
                mmi.ptMaxSize.X = size.Width;
                mmi.ptMaxSize.Y = size.Height;

                Marshal.StructureToPtr(mmi, lParam, true);
            }
            else if (msg == NativeConstants.WmConstants.WM_CLOSE)
            {
                bool success = User32.DestroyWindow(Handle);
                if (!success)
                {
                    throw new Win32Exception();
                }
                return IntPtr.Zero;
            }

            return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
        }
        
    }
}
