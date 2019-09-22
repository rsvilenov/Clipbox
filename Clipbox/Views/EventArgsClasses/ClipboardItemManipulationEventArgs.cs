using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class ClipboardItemManipulationEventArgs : EventArgs
    {
        public ClipboardItemManipulationEventArgs(string clipboardItemId)
        {
            ClipboardItemId = clipboardItemId;
        }

        public string ClipboardItemId { get; set; }
    }
}
