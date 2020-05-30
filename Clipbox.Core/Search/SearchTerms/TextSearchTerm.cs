using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    [Serializable]
    public class TextSearchTerm : SearchTermBase<string>
    {
        public TextSearchTerm(IEnumerable<string> textStrings)
            : base(textStrings)
        { }

        public TextSearchTerm()
            : base()
        { }

        protected TextSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.Text;
    }
}
