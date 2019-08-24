using System.Collections.Generic;

namespace Clipbox.Services.EventArgsClasses
{
    public class PasteRequestedEventArgs : SelectionEventArgs
    {
        public PasteRequestedEventArgs(List<string> selectedItemIds, Dictionary<string, List<int>> selectedMultiItemIds)
            : base(selectedItemIds, selectedMultiItemIds)
        { }

        public bool IsSuccess { get; set; }
    }
}
