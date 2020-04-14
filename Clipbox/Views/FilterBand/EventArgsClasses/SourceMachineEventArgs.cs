using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Views.FilterBand.EventArgsClasses
{
    public class SourceMachineEventArgs
    {
        public SourceMachineEventArgs(IEnumerable<SourceMachineInfo> sourceMachines)
        {
            if (sourceMachines == null)
            {
                throw new ArgumentNullException(nameof(sourceMachines));
            }

            SourceMachines = sourceMachines.ToList().AsReadOnly();
        }

        public IReadOnlyList<SourceMachineInfo> SourceMachines { get; }
    }
}
