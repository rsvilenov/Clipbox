using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class SourceMachineUniqueIdSearchTerm : SearchTermBase<string>
    {
        public SourceMachineUniqueIdSearchTerm(IEnumerable<string> sourceMachineUniqueIds)
            : base(sourceMachineUniqueIds)
        { }

        public SourceMachineUniqueIdSearchTerm()
            : base()
        { }

        protected SourceMachineUniqueIdSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineUniqueId;
    }
}
