using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceApplicationName : SearchTermBase<string>
    {
        public SourceApplicationName(IEnumerable<string> sourceApplicationNames)
        {
            SetSearchValues(sourceApplicationNames);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceApplicationName;
    }
}
