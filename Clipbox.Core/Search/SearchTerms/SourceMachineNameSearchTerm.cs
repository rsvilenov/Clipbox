using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineNameSearchTerm : SearchTermBase<string>
    {
        public SourceMachineNameSearchTerm(IEnumerable<string> sourceMachineNames)
            : base(sourceMachineNames)
        { }

        public SourceMachineNameSearchTerm()
            : base()
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineName;
    }
}
