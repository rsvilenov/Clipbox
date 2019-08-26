using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Clipbox.Services.Interfaces
{
    /// <summary>
    /// Base interface for the clipboard view window services
    /// </summary>
    public interface IClipboardViewWindowServiceBase
    {
        IReadOnlyList<IFlowParameter> GetFlowParameters();

        IReadOnlyList<string> GetSelectedItemIds();

        IReadOnlyDictionary<string, ReadOnlyCollection<int>> GetSelectedMultiItemsIds();

        event EventHandler<SelectionEventArgs> SelectionChanged;

        event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;

        event EventHandler<HousekeepingEventArgs> HousekeepingRequested;
    }
}
