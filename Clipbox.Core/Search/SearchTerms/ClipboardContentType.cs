using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class ClipboardContentType : SearchTermBase<ClipboardContentClass>
    {
        public ClipboardContentType(IEnumerable<ClipboardContentClass> contentClasses)
        {
            SetSearchValues(contentClasses);
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Type;
    }
}
