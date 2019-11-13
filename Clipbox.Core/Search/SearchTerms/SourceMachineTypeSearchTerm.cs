using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceMachineTypeSearchTerm : SearchTermBase<MachineType>
    {
        public SourceMachineTypeSearchTerm(IEnumerable<MachineType> sourceMachineTypes)
            : base(sourceMachineTypes)
        { }

        public SourceMachineTypeSearchTerm()
            : base()
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineType;
    }
}
