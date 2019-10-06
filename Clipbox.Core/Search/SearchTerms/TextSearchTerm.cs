using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class TextSearchTerm : SearchTermBase<string>
    {
        public TextSearchTerm(IEnumerable<string> textStrings)
        {
            SetSearchValues(textStrings);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Text;
    }
}
