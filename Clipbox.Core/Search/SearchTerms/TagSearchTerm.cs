using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class TagSearchTerm : SearchTermBase<string>
    {
        public TagSearchTerm(IEnumerable<string> tags)
            : base(tags)
        { }

        public TagSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Tag;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Tag?.ToString() ?? string.Empty
            };
        }
    }
}
