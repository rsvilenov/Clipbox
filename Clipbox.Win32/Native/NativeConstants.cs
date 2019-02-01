using System;

namespace Clipbox.Win32.Native
{
    public static class NativeConstants
    {

        public static int GWL_EXSTYLE = (-20);
        public static int GWL_STYLE = (-16);



        internal static IntPtr ICON_SMALL2 = new IntPtr(2);
        internal static IntPtr IDI_APPLICATION = new IntPtr(0x7F00);
        internal static int GCL_HICON = -14;

        public static class WmConstants
        {
            public const int WM_DESTROY = 0x2;
            public const int WM_DRAWCLIPBOARD = 0x308;
            public const int WM_CHANGECBCHAIN = 0x30d;
            public const int WM_GETICON = 0x007f;
            public const int WM_DPICHANGED = 0x02E0;
            public const int WM_CLOSE = 0x10;
            public const int WM_USER = 0x400;
            public const int WM_NULL = 0x0;
            public const int WM_COPYDATA = 0x4A;
            public const int WM_SETTINGCHANGE = 0x001A;
            public const int WM_MOUSEACTIVATE = 0x0021;
            public const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
            public const int WM_SHOWWINDOW = 0x0018;
            public const int WM_GETMINMAXINFO = 0x0024;
            public const int WM_PAINT= 0x000F;
            public const int WM_CREATE = 0x0001;
        }

        public static class MonitorConstants
        {
            public const int MONITOR_DEFAULTTONULL = 0;
            public const int MONITOR_DEFAULTTOPRIMARY = 1;
            public const int MONITOR_DEFAULTTONEAREST = 2;
        }

        public static class CwConstants
        {
            public const int CW_USEDEFAULT = unchecked((int)0x80000000);
        }

        // http://www.pinvoke.net/default.aspx/Constants/IDC_.html  
        public static class IDC_Constants
        {
            public const int
             IDC_ARROW = 32512,
             IDC_IBEAM = 32513,
             IDC_WAIT = 32514,
             IDC_CROSS = 32515,
             IDC_UPARROW = 32516,
             IDC_SIZE = 32640,
             IDC_ICON = 32641,
             IDC_SIZENWSE = 32642,
             IDC_SIZENESW = 32643,
             IDC_SIZEWE = 32644,
             IDC_SIZENS = 32645,
             IDC_SIZEALL = 32646,
             IDC_NO = 32648,
             IDC_HAND = 32649,
             IDC_APPSTARTING = 32650,
             IDC_HELP = 32651;
        }
    }
}
