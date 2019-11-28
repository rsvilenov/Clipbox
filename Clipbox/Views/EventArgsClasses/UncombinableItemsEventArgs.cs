using System;

namespace Clipbox.Views.EventArgsClasses
{
    public class UncombinableItemsEventArgs : EventArgs
    {
        public UncombinableItemsEventArgs(string lastSelectedItemId)
        {
            LastSelectedItemId = lastSelectedItemId;
        }

        public string LastSelectedItemId { get; }
    }
}
