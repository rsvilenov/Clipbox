using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class SourceWindowCaptionSearchTerm : SearchTermBase<string>
    {
        public SourceWindowCaptionSearchTerm(IEnumerable<string> sourceWindowCaptionPart)
            : base(sourceWindowCaptionPart)
        { }

        public SourceWindowCaptionSearchTerm()
            : base()
        { }

        protected SourceWindowCaptionSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceWindowName;
    }
}
