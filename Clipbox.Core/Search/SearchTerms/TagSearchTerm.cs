using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class TagSearchTerm : SearchTermBase<string>
    {
        public TagSearchTerm(IEnumerable<string> tags)
            : base(tags)
        { }

        public TagSearchTerm()
            : base()
        { }

        protected TagSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.Tag;
    }
}
