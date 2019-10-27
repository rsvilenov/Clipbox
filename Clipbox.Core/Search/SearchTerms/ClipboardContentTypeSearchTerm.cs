using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

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

        protected override SearchCategory SearchCategory => SearchCategory.Type;
    }
}
