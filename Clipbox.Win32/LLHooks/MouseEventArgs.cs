using Clipbox.Win32.Native;
using System;
using System.Drawing;

namespace Clipbox.Win32.LLHooks
{
    public class MouseEventArgs : EventArgs
    {
        public Point Point { get; set; }
        public NativeEnums.MouseMessages MouseAction { get; set; }
        public bool CancelEvent { get; set; }

        public MouseEventArgs(Point p, NativeEnums.MouseMessages m)
        {
            Point = p;
            MouseAction = m;
        }
    }
}
