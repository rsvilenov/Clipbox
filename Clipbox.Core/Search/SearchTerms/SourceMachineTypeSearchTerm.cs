using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class SourceMachineTypeSearchTerm : SearchTermBase<MachineType>
    {
        public SourceMachineTypeSearchTerm(IEnumerable<MachineType> sourceMachineTypes)
            : base(sourceMachineTypes)
        { }

        public SourceMachineTypeSearchTerm()
            : base()
        { }

        protected SourceMachineTypeSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceMachineType;
    }
}
