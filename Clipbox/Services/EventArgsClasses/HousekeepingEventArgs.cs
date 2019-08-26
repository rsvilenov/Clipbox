using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Services.EventArgsClasses
{
    public class HousekeepingEventArgs : EventArgs
    {
        public HousekeepingEventArgs(IEnumerable<string> clipboardItemIds)
        {
            ClipboardItemIdsToProcess = clipboardItemIds.ToList().AsReadOnly();
            RemovedClipboardItemIds = new List<string>();
        }

        public IReadOnlyCollection<string> ClipboardItemIdsToProcess { get; }

        public IEnumerable<string> RemovedClipboardItemIds { get; set; }
    }
}
