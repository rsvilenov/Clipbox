using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    /// <summary>
    /// Base interface for the clipboard view window services
    /// </summary>
    public interface IClipboardViewWindowServiceBase
    {
        IReadOnlyList<IFlowParameter> GetFlowParameters();
        
        event EventHandler<SelectionEventArgs> SelectionChanged;

        event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;

        event EventHandler<HousekeepingEventArgs> HousekeepingRequested;
    }
}
