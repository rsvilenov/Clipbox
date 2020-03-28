using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class DateRangeEventArgs : EventArgs
    {
        public DateRangeEventArgs(double startMs, double endMs)
        {
            StartMs = startMs;
            EndMs = endMs;
        }

        public double StartMs { get; }
        public double EndMs { get; }
    }
}
