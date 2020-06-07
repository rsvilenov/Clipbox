using System;

namespace Clipbox.Services.Interfaces.Common.EventArgsClasses
{
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int total, int current)
        {
            Total = total;
            Current = current;
        }

        public int Total { get; }

        public int Current { get; }
    }
}
