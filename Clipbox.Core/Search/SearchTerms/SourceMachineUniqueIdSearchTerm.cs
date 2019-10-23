using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineUniqueIdSearchTerm : SearchTermBase<string>
    {
        public SourceMachineUniqueIdSearchTerm(IEnumerable<string> sourceMachineUniqueIds)
            : base(sourceMachineUniqueIds)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineUniqueId;
    }
}
