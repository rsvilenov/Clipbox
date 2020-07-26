using System;

namespace Clipbox.Views.Settings.Interfaces
{
    internal interface ICanRequestAppRestart
    {
        event EventHandler RestartAppRequested;
    }
}
