using System.Collections.Generic;

namespace Clipbox.Services.EventArgsClasses
{
    public class CombinabilityEventArgs : SelectionEventArgs
    {
        public CombinabilityEventArgs(List<string> selectedItemIds, Dictionary<string, List<int>> selectedMultiItemIds)
            : base(selectedItemIds, selectedMultiItemIds)
        { }
        
        public bool AreItemsCombinable { get; set; }
    }
}
