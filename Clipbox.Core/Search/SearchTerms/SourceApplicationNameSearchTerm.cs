using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.IO;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceApplicationNameSearchTerm : SearchTermBase<string>
    {
        public SourceApplicationNameSearchTerm(IEnumerable<string> sourceApplicationNames)
            : base(sourceApplicationNames)
        { }

        public SourceApplicationNameSearchTerm(IClipboardItem clipboardItem)
            : base(clipboardItem)
        { }

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            return new string[]
            {
                Path.GetFileNameWithoutExtension(clipboardItem.Source.SourceWindowInfo.ApplicationPath)
            };
        }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.SourceApplicationName;

    }
}
