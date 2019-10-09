using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search.SearchTerms
{
    public class ClipboardContentTypeSearchTerm : SearchTermBase<ClipboardContentClass>
    {
        public ClipboardContentTypeSearchTerm(IEnumerable<ClipboardContentClass> contentClasses)
            : base(contentClasses)
        { }

        public ClipboardContentTypeSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override IEnumerable<ClipboardContentClass> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return clipboardItem
                .ClipboardDataObjects
                .Select(cdo => cdo.ContentClass)
                .Distinct();
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Type;
    }
}
