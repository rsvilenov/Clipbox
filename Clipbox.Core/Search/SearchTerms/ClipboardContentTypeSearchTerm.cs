using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search.SearchTerms
{
    public class ClipboardContentTypeSearchTerm : SearchTermBase<ClipboardContentClass>
    {
        public ClipboardContentTypeSearchTerm(IEnumerable<ClipboardContentClass> contentClasses)
            : base(contentClasses)
        { }

        public ClipboardContentTypeSearchTerm()
            : base()
        { }

        protected ClipboardContentTypeSearchTerm(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.Type;
    }
}
