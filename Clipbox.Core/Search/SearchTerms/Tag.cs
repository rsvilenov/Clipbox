using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class Tag : SearchTermBase<string>
    {
        public Tag(IEnumerable<string> tags)
        {
            SetSearchValues(tags);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Tag;
    }
}
