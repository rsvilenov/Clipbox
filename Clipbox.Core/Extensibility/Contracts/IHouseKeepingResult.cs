using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IHouseKeepingResult
    {
        bool ShouldRemoveItem { get; }

        IEnumerable<int> ClipboardDataObjectsToRemove { get; }
    }
}
