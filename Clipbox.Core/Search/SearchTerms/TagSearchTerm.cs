using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class TagSearchTerm : SearchTermBase<string>
    {
        public TagSearchTerm(IEnumerable<string> tags)
            : base(tags)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.Tag;
    }
}
