using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class ClipboardItemGroupManipulationEventArgs : EventArgs
    {
        public ClipboardItemGroupManipulationEventArgs(DateTime day)
        {
            Day = day;
        }

        public DateTime Day { get; }
    }
}
