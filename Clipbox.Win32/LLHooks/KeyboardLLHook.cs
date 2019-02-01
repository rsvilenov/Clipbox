using Clipbox.Win32.Native;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Clipbox.Win32.LLHooks
{
    public class KeyboardLLHook : HookBase
    {
        public event Action<KeyboardEventArgs> KeyboardAction;

        protected override NativeEnums.HookType TypeOfHook
        {
            get
            {
                return NativeEnums.HookType.WH_KEYBOARD_LL;
            }
        }

        protected override CallbackResult OnCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)NativeEnums.KeyboardMessages.WM_KEYDOWN || wParam == (IntPtr)NativeEnums.KeyboardMessages.WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var args = new KeyboardEventArgs((NativeEnums.Keys)vkCode, (NativeEnums.KeyboardMessages)wParam);
                KeyboardAction?.Invoke(args);
                if (args.CancelEvent)
                {
                    Debug.WriteLine("CANCELLED!!!");
                    return CallbackResult.FromIntPtr(new IntPtr(1));
                }
            }

            return CallbackResult.DefaultResult;
        }
    }
}
