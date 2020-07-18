using System.ComponentModel;

namespace Clipbox.Views.EventArgsClasses
{
    public class ClipboardItemManipulationCancelEventArgs : CancelEventArgs
    {
        public ClipboardItemManipulationCancelEventArgs(string clipboardItemId)
        {
            ClipboardItemId = clipboardItemId;
        }

        public string ClipboardItemId { get; set; }
    }
}
