using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Services
{
    public abstract class ClipboardViewWindowServiceBase : IClipboardViewWindowServiceBase
    {
        public ClipboardViewWindowServiceBase()
        {
            SelectedItemIds = new List<string>();
            SelectedMultiItemIds = new Dictionary<string, ReadOnlyCollection<int>>();
        }

        protected List<string> SelectedItemIds { get; set; }

        public IReadOnlyList<string> GetSelectedItemIds()
        {
            return SelectedItemIds.AsReadOnly();
        }

        protected Dictionary<string, ReadOnlyCollection<int>> SelectedMultiItemIds { get; set; }

        public IReadOnlyDictionary<string, ReadOnlyCollection<int>> GetSelectedMultiItemsIds()
        {
            return new ReadOnlyDictionary<string, ReadOnlyCollection<int>>(SelectedMultiItemIds);
        }
        
        public event EventHandler<SelectionEventArgs> SelectionChanged;

        protected void RaiseSelectionChangedEvent(SelectionEventArgs args)
        {
            SelectionChanged?.Invoke(this, args);
        }
        
        public event EventHandler<CombinabilityEventArgs> CombinabilityCheckRequested;

        protected void RaiseCombinabilityCheckRequestedEvent(CombinabilityEventArgs args)
        {
            CombinabilityCheckRequested?.Invoke(this, args);
        }

        public abstract IReadOnlyList<IFlowParameter> GetFlowParameters();
    }
}
