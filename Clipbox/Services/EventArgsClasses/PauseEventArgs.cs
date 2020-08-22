using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class PauseEventArgs : EventArgs
    {
        public PauseEventArgs(bool isPaused, long pauseIntervalMs)
        {
            IsPaused = isPaused;
            PauseIntervalMs = pauseIntervalMs;
        }

        public bool IsPaused { get; }

        public long PauseIntervalMs { get; }
    }
}
