using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceWindowCaptionSearchTerm : SearchTermBase<string>
    {
        public SourceWindowCaptionSearchTerm(IEnumerable<string> sourceWindowName)
        {
            SetSearchValues(sourceWindowName);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceWindowName;
    }
}
