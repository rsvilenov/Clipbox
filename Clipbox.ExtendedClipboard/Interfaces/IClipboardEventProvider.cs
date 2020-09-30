using Clipbox.ExtendedClipboard.EventArgs;
using System;

namespace Clipbox.ExtendedClipboard.Interfaces
{
    public interface IClipboardEventProvider
    {
        event EventHandler<ClipboardEventArgs> ClipboardContentChanged;

        IntPtr GetEventWindowHandle();
    }
}
