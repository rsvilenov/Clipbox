using Clipbox.Win32.Native;

namespace Clipbox.Win32
{
    public static class KeyboardUtils
    {
        public static bool IsKeyPressed(NativeEnums.Keys key)
        {
            // check the high-order bit as shown here: https://stackoverflow.com/questions/8995624/getkeystate-not-evaluating-correctly
            // and here: https://www.pinvoke.net/default.aspx/user32.getkeyboardstate
            return (User32.GetKeyState((int)key) & 0x8000) != 0;
        }

        // https://stackoverflow.com/questions/14395377/how-to-simulate-a-ctrl-a-ctrl-c-using-keybd-event
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001; // Key down flag
        private const int KEYEVENTF_KEYUP = 0x0002; // Key up flag

        public static void SimulateCtrlV()
        {
            //User32.keybd_event((byte)NativeEnums.Keys.LeftControl, 0, KEYEVENTF_EXTENDEDKEY, 0);
            //User32.keybd_event((byte)NativeEnums.Keys.V, 0, KEYEVENTF_EXTENDEDKEY, 0);
            //User32.keybd_event((byte)NativeEnums.Keys.V, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            //User32.keybd_event((byte)NativeEnums.Keys.LeftControl, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);

            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms646307(v=vs.85).aspx
            // https://stackoverflow.com/questions/21197257/keybd-event-keyeventf-extendedkey-explanation-required
            uint lCtrlScanCode = User32.MapVirtualKey((uint)NativeEnums.Keys.LeftControl, (uint)NativeEnums.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);
            uint vScanCode = User32.MapVirtualKey((uint)NativeEnums.Keys.V, (uint)NativeEnums.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);

            User32.keybd_event((byte)NativeEnums.Keys.LeftControl, (byte)lCtrlScanCode, 0, 0);
            User32.keybd_event((byte)NativeEnums.Keys.V, (byte)vScanCode, 0, 0);
            User32.keybd_event((byte)NativeEnums.Keys.V, (byte)vScanCode, KEYEVENTF_KEYUP, 0);
            User32.keybd_event((byte)NativeEnums.Keys.LeftControl, (byte)lCtrlScanCode, KEYEVENTF_KEYUP, 0);
        }

        public static void SimulateV()
        {
            uint vScanCode = User32.MapVirtualKey((uint)NativeEnums.Keys.V, (uint)NativeEnums.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);

            User32.keybd_event((byte)NativeEnums.Keys.V, (byte)vScanCode, 0, 0);
            User32.keybd_event((byte)NativeEnums.Keys.V, (byte)vScanCode, KEYEVENTF_KEYUP, 0);
        }

        public static void SimulateKeyUp(NativeEnums.Keys key)
        {
            uint scanCode = User32.MapVirtualKey((uint)key, (uint)NativeEnums.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);

            User32.keybd_event((byte)key, (byte)scanCode, KEYEVENTF_KEYUP, 0);
        }

        public static void SimulateEnter()
        {
            uint vScanCode = User32.MapVirtualKey((uint)NativeEnums.Keys.Enter, (uint)NativeEnums.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);

            User32.keybd_event((byte)NativeEnums.Keys.Enter, (byte)vScanCode, 0, 0);
            User32.keybd_event((byte)NativeEnums.Keys.Enter, (byte)vScanCode, KEYEVENTF_KEYUP, 0);
        }

    }
}
