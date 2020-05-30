using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class SourceMachineNameSearchTerm : SearchTermBase<string>
    {
        public SourceMachineNameSearchTerm(IEnumerable<string> sourceMachineNames)
            : base(sourceMachineNames)
        { }

        public SourceMachineNameSearchTerm()
            : base()
        { }

        protected SourceMachineNameSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineName;
    }
}
