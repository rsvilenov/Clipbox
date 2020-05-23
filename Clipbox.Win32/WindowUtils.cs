using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace Clipbox.Win32
{
    public static class WindowUtils
    {
        /// <summary>
        /// Changes the coordinates and z-index of the window
        /// without activating it. Use this call instead of SetForegroundWindow
        /// when you want to set the window as topmost but without activating it.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="keepZOrder">False means 'set TOPMOST'</param>
        public static void SetWindowsPosition(IntPtr hWnd, int x, int y, int width, int height, bool keepZOrder = true)
        {
            NativeEnums.SetWindowPosFlags flags = NativeEnums.SetWindowPosFlags.SWP_NOACTIVATE | NativeEnums.SetWindowPosFlags.SWP_FRAMECHANGED;
            IntPtr insertAfter = IntPtr.Zero;
            if (keepZOrder)
            {
                flags = NativeEnums.SetWindowPosFlags.SWP_NOZORDER | NativeEnums.SetWindowPosFlags.SWP_NOOWNERZORDER;
            }
            else
            {
                insertAfter = new IntPtr(-1); // HWND_TOPMOST
            }
            int ret = User32.SetWindowPos(hWnd, (int)insertAfter, x, y, width, height, (int)flags);
            if (ret == 0)
                MarshalExt.ThrowOnWin32Error();
        }

        public static int GetPidByHWnd(IntPtr hWnd)
        {
            uint processId;
            int threadId = (int)User32.GetWindowThreadProcessId(hWnd, out processId);
            if (processId == 0)
            {
                MarshalExt.ThrowOnWin32Error();
            }
            return (int)processId;
        }

        public static string GetWindowCaptionText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = User32.GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            User32.GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// 64 bit version may lose significant 64-bit specific information
        /// </summary>
        private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                // IntPtr constructor on 32 systems uses checked int, but GetClassLong32 returns uint and OverflowException occurs.
                // A workaround was found in this commit https://github.com/WernerWenz/opentk/commit/d9e05bec4551973c59aa55ef4ec363d4923e08d4
                // which was mentioned in this discussion of the .NET Core project: https://github.com/dotnet/coreclr/issues/4062
                return (IntPtr)unchecked((int)(User32.GetClassLong32(hWnd, nIndex)));
            else
                return User32.GetClassLong64(hWnd, nIndex);
        }
        
        public static Bitmap GetSmallWindowIcon(IntPtr hWnd)
        {
            try
            {
                IntPtr hIcon = default(IntPtr);
                // try to get the big icon
                hIcon = User32.SendMessage(hWnd, NativeConstants.WmConstants.WM_GETICON, new IntPtr((int)NativeEnums.GET_ICON_TYPE.ICON_BIG), IntPtr.Zero);

                // try to get the big icon with GetClassLongPtr
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, NativeConstants.GCL_HICON);

                // still nothing? try getting the small icon with wm_geticon
                if (hIcon == IntPtr.Zero)
                    hIcon = User32.SendMessage(hWnd, NativeConstants.WmConstants.WM_GETICON, new IntPtr((int)NativeEnums.GET_ICON_TYPE.ICON_SMALL2), IntPtr.Zero);

                // failed? try LoadIcon
                if (hIcon == IntPtr.Zero)
                    hIcon = User32.LoadIcon(IntPtr.Zero, (IntPtr)0x7F00/*IDI_APPLICATION*/);

                if (hIcon != IntPtr.Zero)
                    return new Bitmap(Icon.FromHandle(hIcon).ToBitmap(), 16, 16);
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Bitmap GetSmallIconFromDirectoryPath(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                // use this method only for directories, as we have much better ways to
                // get the icons for the files.
                throw new ArgumentException("The path does not exist or is not a directory.");
            }

            // https://stackoverflow.com/questions/23666076/how-to-get-the-icon-associated-with-a-specific-folder

            //Constants flags for SHGetFileInfo 
            const uint SHGFI_ICON = 0x100;
            //const uint SHGFI_LARGEICON = 0x000000000; // Large icon
            const uint SHGFI_SMALLICON = 0x000000001; // Small icon

            NativeStructures.SHFILEINFO shinfo = new NativeStructures.SHFILEINFO();
            
            Shell32.SHGetFileInfo(
                directoryPath,
                0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON);

            using (Icon i = Icon.FromHandle(shinfo.hIcon))
            {
                return i.ToBitmap();
            }
        }

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            Kernel32.SetLastError(0);
            if (IntPtr.Size == 8)
            {
                // use SetWindowLongPtr
                result = User32.IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }
            else
            {
                // use SetWindowLong
                UInt32 tempResult = User32.IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new Win32Exception(error);
            }

            return result;
        }

        public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 GetWindowLong may nog clear  the error on success
            Kernel32.SetLastError(0);
            if (IntPtr.Size == 8)
            {
                // use GetWindowLongPtr
                result = User32.GetWindowLongPtr64(hWnd, nIndex);
            }
            else
            {
                // use GetWindowLong
                result = User32.GetWindowLongPtr32(hWnd, nIndex);
            }

            error = Marshal.GetLastWin32Error();
            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new Win32Exception(error);
            }

            return result;
        }

        public static void AddWindowStyleEx(IntPtr hWnd, NativeEnums.WindowStylesEx style)
        {
            ToggleWindowStyleEx(hWnd, style, false);
        }

        public static void AddWindowStyle(IntPtr hWnd, NativeEnums.WindowStyles style)
        {
            ToggleWindowStyle(hWnd, style, false);
        }

        public static void RemoveWindowStyleEx(IntPtr hWnd, NativeEnums.WindowStylesEx style)
        {
            ToggleWindowStyleEx(hWnd, style, true);
        }

        public static void RemoveWindowStyle(IntPtr hWnd, NativeEnums.WindowStyles style)
        {
            ToggleWindowStyle(hWnd, style, true);
        }

        public static void SetTopMostNoActivate(IntPtr hWnd)
        {
            var windowRect = WindowUtils.GetWindowRectangle(hWnd);
            
            SetWindowsPosition(hWnd, windowRect.Left, windowRect.Top,
                    windowRect.Width,
                    windowRect.Height, keepZOrder: false);

        }

        private static void ToggleWindowStyleEx(IntPtr hWnd, NativeEnums.WindowStylesEx style, bool remove)
        {
            NativeEnums.WindowStylesEx exStyle = GetWindowStylesEx(hWnd);

            bool flagAlreadyExists = exStyle.HasFlag(style);
            if (remove)
            {
                if (flagAlreadyExists)
                {
                    exStyle &= ~style;
                }
                else
                {
                    throw new InvalidOperationException($"Window with hWnd {hWnd} does not have the style {style}");
                }
            }
            else
            {
                if (!flagAlreadyExists)
                {
                    exStyle |= style;
                }
                else
                {
                    throw new InvalidOperationException($"Window with hWnd {hWnd} already has the style {style}");
                }
            }

            SetWindowLong(hWnd, (int)NativeConstants.GWL_EXSTYLE, (IntPtr)exStyle);
            
        }

        private static void ToggleWindowStyle(IntPtr hWnd, NativeEnums.WindowStyles style, bool remove)
        {
            NativeEnums.WindowStyles exStyle = GetWindowStyles(hWnd);

            bool flagAlreadyExists = exStyle.HasFlag(style);//(exStyle & NativeEnums.WindowStyles.WS_EX_TOPMOST) == style;
            if (remove)
            {
                if (flagAlreadyExists)
                {
                    exStyle &= ~style;
                }
                else
                {
                    throw new InvalidOperationException($"Window with hWnd {hWnd} does not have the style {style}");
                }
            }
            else
            {
                if (!flagAlreadyExists)
                {
                    exStyle |= style;
                }
                else
                {
                    throw new InvalidOperationException($"Window with hWnd {hWnd} already has the style {style}");
                }
            }

            IntPtr newExStylePtr = SetWindowLong(hWnd, (int)NativeConstants.GWL_STYLE, (IntPtr)exStyle);
            if (newExStylePtr == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }

        public static bool CheckWindowStyleIsSet(IntPtr hWnd, NativeEnums.WindowStylesEx style)
        {
            NativeEnums.WindowStylesEx exStyle = GetWindowStylesEx(hWnd);

            return exStyle.HasFlag(style);
        }

        private static NativeEnums.WindowStylesEx GetWindowStylesEx(IntPtr hWnd)
        {
            IntPtr exStylePtr = GetWindowLong(hWnd, NativeConstants.GWL_EXSTYLE);
            long exStyle = 0;
            if (IntPtr.Size == 4)
            {
                exStyle = exStylePtr.ToInt32();
            }
            else
            {
                exStyle = exStylePtr.ToInt64();
            }
            return (NativeEnums.WindowStylesEx)exStyle;
        }

        private static NativeEnums.WindowStyles GetWindowStyles(IntPtr hWnd)
        {
            IntPtr stylePtr = GetWindowLong(hWnd, (int)NativeConstants.GWL_STYLE);
            long style = 0;
            if (IntPtr.Size == 4)
            {
                style = stylePtr.ToInt32();
            }
            else
            {
                style = stylePtr.ToInt64();
            }
            return (NativeEnums.WindowStyles)style;
        }

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        public class WindowInfo : IDisposable
        {
            public string ApplicationPath { get; internal set; }

            public string Caption { get; internal set; }

            public Bitmap Icon { get; internal set; }

            public void Dispose()
            {
                if (Icon != null)
                {
                    Icon.Dispose();
                }
            }
        }

        public static Rectangle GetWindowRectangle(IntPtr hWnd)
        {
            NativeStructures.RECT rect;
            if (!User32.GetWindowRect(hWnd, out rect))
            {
                throw new Win32Exception();
            }

            return rect.ToRectangle();
        }

        /// <summary>
        /// Gets the foremost (currently active) window hwnd
        /// <exception>Win32Exception</exception>
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetForemostWindowHwnd()
        {
            var info = new NativeStructures.GuiThreadInfo();
            info.cbSize = Marshal.SizeOf(info);
            // in apps, hosted inside an ApplicationFrameHost, we need
            // to obtain the actual window.
            if (!User32.GetGUIThreadInfo(0, ref info))
            {
                // if GetGUIThreadInfo fails, try with GetForegroundWindow()

                // MSDN: Retrieves a handle to the foreground window 
                // (the window with which the user is currently working).
                // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow
                return User32.GetForegroundWindow();
            }

            return info.hwndFocus;
        }

        /// <summary>
        /// Gets the window info for the specified HWND
        /// <exception>Win32Exception</exception>
        /// </summary>
        /// <returns></returns>

        public static WindowInfo GetWindowInfo(IntPtr hwnd)
        {
            var applicationPid = GetPidByHWnd(hwnd);
            string windowCaption = GetWindowCaptionText(hwnd);

            //using (Process applicationProcess = Process.GetProcessById(applicationPid))
            //{
            //applicationProcess.MainModule.FileName; // this program should be 64 bit in order for this to work
            //}
            string applicationPath = ProcessUtils.GetExecutablePath(applicationPid);

            WindowInfo windowInfo = new WindowInfo
            {
                ApplicationPath = applicationPath
            };

            if (string.IsNullOrWhiteSpace(windowCaption)
                && !string.IsNullOrWhiteSpace(applicationPath)
                && applicationPath.All(c => !Path.GetInvalidPathChars().Contains(c))
                && Path.GetFileNameWithoutExtension(applicationPath).Equals("explorer", StringComparison.OrdinalIgnoreCase))
            {
                windowInfo.Caption = "Desktop";
                windowInfo.Icon = GetSmallIconFromDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }
            else if (!string.IsNullOrWhiteSpace(applicationPath)
                && applicationPath.All(c => !Path.GetInvalidPathChars().Contains(c))
                && ApplicationUtils.CheckIsWindowsApp(applicationPath))
            {
                windowInfo.Caption = windowCaption;
                windowInfo.Icon = ApplicationUtils.GetModernAppIcon(applicationPath);
            }
            else
            {
                windowInfo.Caption = windowCaption;
                windowInfo.Icon = GetSmallWindowIcon(hwnd);
            }
            
            return windowInfo;
        }

        

        /// <summary>
        /// Gets the screen position of the caret
        /// </summary>
        /// <param name="hWnd">The caret may not be inside the foreground window, as a window can be shown in the foreground without activation</param>
        /// <returns></returns>
        public static Point GetCaretPosition(IntPtr hWnd)
        {
            Point p = new Point();
            
            if (hWnd != IntPtr.Zero)
            {
                uint pid = 0;
                // this (bad casts, etc...) should be revised... 
                int targetThreadID = (int)User32.GetWindowThreadProcessId(hWnd, out pid);
                int localThreadID = (int)Kernel32.GetCurrentThreadId();

                if (localThreadID != targetThreadID)
                {
                    User32.AttachThreadInput(localThreadID, targetThreadID, 1);
                    hWnd = User32.GetFocus();
                    if (hWnd != IntPtr.Zero)
                    {
                        User32.GetCaretPos(out p);
                        User32.ClientToScreen(hWnd, ref p);
                    }
                    User32.AttachThreadInput(localThreadID, targetThreadID, 0);
                }
            }
            return p;
        }
    }
}
