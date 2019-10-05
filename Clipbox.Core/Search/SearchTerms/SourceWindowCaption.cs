using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceWindowCaption : SearchTermBase<string>
    {
        public SourceWindowCaption(IEnumerable<string> sourceWindowName)
        {
            SetSearchValues(sourceWindowName);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceWindowName;
    }
}
