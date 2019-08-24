using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Clipbox.Services.EventArgsClasses
{
    public class SelectionEventArgs : EventArgs
    {
        public SelectionEventArgs(List<string> selectedItemIds, Dictionary<string, List<int>> selectedMultiItemIds)
        {
            SelectedItemIds = selectedItemIds.AsReadOnly();

            var multiItemsTransformedValuesDic = selectedMultiItemIds.ToDictionary(k => k.Key, v => v.Value.AsReadOnly() as IReadOnlyCollection<int>);
            SelectedMultiItemIds = new ReadOnlyDictionary<string, IReadOnlyCollection<int>>(multiItemsTransformedValuesDic);
        }

        public IReadOnlyCollection<string> SelectedItemIds { get; }
        
        public IReadOnlyDictionary<string, IReadOnlyCollection<int>> SelectedMultiItemIds { get; }
    }
}
