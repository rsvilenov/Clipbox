using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceWindowCaptionSearchTerm : SearchTermBase<string>
    {
        public SourceWindowCaptionSearchTerm(IEnumerable<string> sourceWindowName)
            : base(sourceWindowName)
        { }

        public SourceWindowCaptionSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                clipboardItem.Source.SourceWindowInfo.Caption
            };
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceWindowName;
    }
}
