using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Clipbox.Services.EventArgsClasses
{
    public class SelectionEventArgs : CancelEventArgs
    {
        public IEnumerable<string> SelectedItemIds { get; set; }

        public string LastSelectedItemId { get; set; }

        public string CancelMessage { get; set; }
    }
}
