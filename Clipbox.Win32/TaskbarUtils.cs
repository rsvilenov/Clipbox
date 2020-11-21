using Clipbox.Win32.Native;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Clipbox.Win32
{
    public static class TaskbarUtils
    {
        private const int ABM_GETTASKBARPOS = 5;

        public static TaskbarInfo GetTaskbarInfo()
        {
            NativeStructures.APPBARDATA data = new NativeStructures.APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);
            Shell32.SHAppBarMessage(ABM_GETTASKBARPOS, ref data);

            return new TaskbarInfo
            {
                Rectangle = data.rc.ToRectangle(),
                TaskbarHWnd = data.hWnd
            };
        }

        public struct TaskbarInfo
        {
            public Rectangle Rectangle { get; internal set; }
            public IntPtr TaskbarHWnd { get; internal set; }
        }
    }
}
