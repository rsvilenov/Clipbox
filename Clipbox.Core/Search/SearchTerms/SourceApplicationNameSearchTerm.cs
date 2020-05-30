using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class SourceApplicationNameSearchTerm : SearchTermBase<string>
    {
        public SourceApplicationNameSearchTerm(IEnumerable<string> sourceApplicationNames)
            : base(sourceApplicationNames)
        { }

        public SourceApplicationNameSearchTerm()
            : base()
        { }

        protected SourceApplicationNameSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceApplicationName;

    }
}
