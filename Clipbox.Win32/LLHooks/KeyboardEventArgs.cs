using Clipbox.Win32.Native;

namespace Clipbox.Win32.LLHooks
{
    public class KeyboardEventArgs
    {
        public NativeEnums.KeyboardMessages KeyboardAction { get; set; }
        public NativeEnums.Keys Key { get; set; }
        public bool CancelEvent { get; set; }

        public KeyboardEventArgs(NativeEnums.Keys key, NativeEnums.KeyboardMessages action)
        {
            Key = key;
            KeyboardAction = action;
        }
    }
}
