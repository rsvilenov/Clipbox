using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using static Clipbox.Win32.Native.NativeEnums;
using static Clipbox.Win32.Native.NativeStructures;

namespace Clipbox.Win32.Native
{
    public static class User32
    {
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr GetClipboardOwner();

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern uint EnumClipboardFormats(uint format);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport(ExternDll.User32)]
        public static extern bool CloseClipboard();

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);


        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool SetClipboardData(uint uFormat, IntPtr data);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool EmptyClipboard();

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern uint RegisterClipboardFormat(string lpszFormat);
        
        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32, SetLastError= true, CharSet= CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            SendMessageTimeoutFlags fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);
        
        [DllImport(ExternDll.User32)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(ExternDll.User32)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);


        [DllImport(ExternDll.User32)]
        public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport(ExternDll.User32, EntryPoint = "GetClassLong")]
        public static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

        [DllImport(ExternDll.User32, EntryPoint = "GetClassLongPtr")]
        public static extern IntPtr GetClassLong64(IntPtr hWnd, int nIndex);

        [SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable", MessageId = "return", Justification = "Calling code is expected to handle the different size of IntPtr")]
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        internal static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowLong", SetLastError = true)]
        internal static extern UInt32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);


        public delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport(ExternDll.User32)]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        [DllImport(ExternDll.User32, EntryPoint = "SetWindowPos")]
        public static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);


        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public extern static bool DestroyIcon(IntPtr handle);

        [DllImport(ExternDll.User32)]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport(ExternDll.User32)]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport(ExternDll.User32)]
        public static extern bool GetCaretPos(out Point lpPoint);

        [DllImport(ExternDll.User32)]
        public static extern IntPtr GetFocus();

        [DllImport(ExternDll.User32)]
        public static extern IntPtr AttachThreadInput(int idAttach, int idAttachTo, int fAttach);

        [DllImport(ExternDll.User32)]
        public static extern int ClientToScreen(IntPtr hWnd, ref Point p);

        [DllImport(ExternDll.User32)]
        public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        [DllImport(ExternDll.User32)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, ChangeWindowMessageFilterExAction action, ref CHANGEFILTERSTRUCT changeInfo);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(UInt32 uAction, int uParam, string lpvParam, int fuWinIni);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool PaintDesktop(IntPtr hdc);

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASS
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern UInt16 RegisterClassW(
            [In] ref WNDCLASS lpWndClass
        );

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr CreateWindowExW(
        UInt32 dwExStyle,
        [MarshalAs(UnmanagedType.LPWStr)]
        string lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)]
        string lpWindowName,
        UInt32 dwStyle,
        Int32 x,
        Int32 y,
        Int32 nWidth,
        Int32 nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr DefWindowProcW(
            IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam
        );

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool DestroyWindow(
            IntPtr hWnd
        );

        [DllImport(ExternDll.User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport(ExternDll.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref Point pt);

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr MonitorFromPoint(Point pt, MonitorOptions dwFlags);

        [DllImport(ExternDll.User32, CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetDoubleClickTime();

        [DllImport(ExternDll.User32, SetLastError = true)]
        internal static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo lpgui);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport(ExternDll.User32, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport(ExternDll.User32, ExactSpelling = true)]
        internal static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        [DllImport(ExternDll.User32, SetLastError = false)]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport(ExternDll.User32)]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    }
}
