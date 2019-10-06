using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class Text : SearchTermBase<string>
    {
        public Text(IEnumerable<string> textStrings)
        {
            SetSearchValues(textStrings);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Text;
    }
}
