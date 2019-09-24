using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.Services
{
    public abstract class ClipboardViewWindowServiceBase : IClipboardViewWindowServiceBase
    {
        public event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;

        protected void RaiseCombinabilityCheckRequestedEvent(CombinabilityEventArgs args)
        {
            CombinabilityCheckRequested?.Invoke(this, args);
        }
        
        public event EventHandler<HousekeepingEventArgs> HousekeepingRequested;

        protected void RaiseHousekeepingRequestedEvent(HousekeepingEventArgs args)
        {
            HousekeepingRequested?.Invoke(this, args);
        }

        public abstract IReadOnlyList<IFlowParameter> GetFlowParameters();
    }
}
