using Clipbox.Services.Interfaces;
using Clipbox.Win32;
using Clipbox.Win32.LLHooks;
using System;
using System.ComponentModel;

namespace Clipbox.Services
{
    internal class LowLevelKeyboardService : ILowLevelKeyboardService, IDisposable
    {
        private KeyboardLLHook _keyboardHook;

        public event EventHandler<CancelEventArgs> ControlKeyUp;

        public event EventHandler<CancelEventArgs> VKeyUp;

        public event EventHandler<CancelEventArgs> CtrlVHotkeyDown;

        private bool _simulatingCtrlV = false;

        public LowLevelKeyboardService()
        {
            _keyboardHook = new KeyboardLLHook();

            _keyboardHook.KeyboardAction += _keyboardHook_KeyboardAction;
            _keyboardHook.Start();

        }

        private void _keyboardHook_KeyboardAction(KeyboardEventArgs args)
        {
            // Ctrl up
            if (args.KeyboardAction == Win32.Native.NativeEnums.KeyboardMessages.WM_KEYUP &&
                (args.Key == Win32.Native.NativeEnums.Keys.LeftControl || args.Key == Win32.Native.NativeEnums.Keys.RightContol))
            {
                if (_simulatingCtrlV)
                {
                    _simulatingCtrlV = false;
                    return;
                }

                CancelEventArgs ctrlKeyUpArgs = new CancelEventArgs();
                ControlKeyUp?.Invoke(this, ctrlKeyUpArgs);
                args.CancelEvent = ctrlKeyUpArgs.Cancel;
            }
            // V up
            else if (args.KeyboardAction == Win32.Native.NativeEnums.KeyboardMessages.WM_KEYUP &&
                args.Key == Win32.Native.NativeEnums.Keys.V)
            {
                if (_simulatingCtrlV)
                {
                    _simulatingCtrlV = false;
                    return;
                }
                CancelEventArgs vKeyUpArgs = new CancelEventArgs();
                VKeyUp?.Invoke(this, vKeyUpArgs);
                args.CancelEvent = vKeyUpArgs.Cancel;
            }
            // Ctrl + V down
            else if (args.KeyboardAction == Win32.Native.NativeEnums.KeyboardMessages.WM_KEYDOWN &&
                (KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.LeftControl) || KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.RightContol)) &&
                args.Key == Win32.Native.NativeEnums.Keys.V)
            {
                if (_simulatingCtrlV)
                {
                    return;
                }

                CancelEventArgs ctrlVArgs = new CancelEventArgs();
                CtrlVHotkeyDown?.Invoke(this, ctrlVArgs);

                // if needed, prevent the key from reaching the target window
                args.CancelEvent = ctrlVArgs.Cancel;
            }
        }

        public void Dispose()
        {
            _keyboardHook.Dispose();
        }

        public void SimulateCtrlV()
        {
            _simulatingCtrlV = true;

            // Ctrl key may already be down, so we have to check this
            if (KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.LeftControl)
                || KeyboardUtils.IsKeyPressed(Win32.Native.NativeEnums.Keys.RightContol))
            {

                KeyboardUtils.SimulateV();
            }
            else
            {
                KeyboardUtils.SimulateCtrlV();
            }
        }
    }
}
