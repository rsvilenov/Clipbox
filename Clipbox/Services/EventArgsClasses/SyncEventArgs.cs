using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class SyncEventArgs : EventArgs
    {
        public SyncEventArgs(string clipboardItemId)
        {
            ClipboardItemId = clipboardItemId;
        }

        public string ClipboardItemId { get; private set; }
    }
}
