using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineNameSearchTerm : SearchTermBase<string>
    {
        public SourceMachineNameSearchTerm(IEnumerable<string> sourceMachineNames)
        {
            SetSearchValues(sourceMachineNames);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceMachineName;
    }
}
