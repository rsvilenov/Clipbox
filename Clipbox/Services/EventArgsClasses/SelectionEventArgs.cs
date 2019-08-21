using System;
using System.Collections.Generic;

namespace Clipbox.Services.EventArgsClasses
{
    public class SelectionEventArgs : EventArgs
    {
        public IEnumerable<string> SelectedItemIds { get; set; }

        public bool AreItemsCombinable { get; set; }
    }
}
