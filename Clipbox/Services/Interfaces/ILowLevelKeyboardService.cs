using System;
using System.ComponentModel;

namespace Clipbox.Services.Interfaces
{
    public interface ILowLevelKeyboardService
    {
        event EventHandler<CancelEventArgs> ControlKeyUp;

        event EventHandler<CancelEventArgs> VKeyUp;
        
        event EventHandler<CancelEventArgs> CtrlVHotkeyDown;

        void SimulateCtrlV();
    }
}
