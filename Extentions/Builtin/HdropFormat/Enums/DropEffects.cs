using System;

namespace Clipbox.Extentions.BuiltIn.HdropFormat.Enums
{

    // https://msdn.microsoft.com/en-us/library/windows/desktop/ms693457(v=vs.85).aspx
    [Flags]
    public enum DropEffects : uint
    {
        DROPEFFECT_NONE = 0,
        DROPEFFECT_COPY = 1,
        DROPEFFECT_MOVE = 2,
        DROPEFFECT_LINK = 4,
        DROPEFFECT_SCROLL = 0x80000000
    }
}
