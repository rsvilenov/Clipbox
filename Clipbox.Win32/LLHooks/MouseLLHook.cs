using Clipbox.Win32.Native;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Clipbox.Win32.LLHooks
{
    public class MouseLLHook : HookBase
    {
        public event Action<MouseEventArgs> MouseAction;

        protected override NativeEnums.HookType TypeOfHook
        {
            get
            {
                return NativeEnums.HookType.WH_MOUSE_LL;
            }
        }

        protected override CallbackResult OnCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            NativeStructures.MSLLHOOKSTRUCT hookStruct = (NativeStructures.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeStructures.MSLLHOOKSTRUCT));
            MouseEventArgs args = new MouseEventArgs(new Point(hookStruct.pt.X, hookStruct.pt.Y), (NativeEnums.MouseMessages)wParam);

            if (nCode >= 0)
            {
                MouseAction?.Invoke(args);
            }

            if (args.CancelEvent)
            {
                return CallbackResult.FromIntPtr(new IntPtr(1));
            }

            return CallbackResult.DefaultResult;
        }
        
    }
}
