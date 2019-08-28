using System.Collections.Generic;

namespace Clipbox.Services.EventArgsClasses
{
    public class SetToClipboardEventArgs : SelectionEventArgs
    {
        public SetToClipboardEventArgs(List<string> selectedItemIds, 
            Dictionary<string, List<int>> selectedMultiItemIds,
            bool keepGeneratedItem)
            : base(selectedItemIds, selectedMultiItemIds)
        {
            KeepGeneratedItem = keepGeneratedItem;
        }

        /// <summary>
        /// Controls whether we should keep the newly created (or combined) items (if any) by the flow extensions...
        /// </summary>
        public bool KeepGeneratedItem { get; set; }
    }
}
