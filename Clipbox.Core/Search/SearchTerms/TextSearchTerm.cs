using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    /// <summary>
    /// This search term can be constructed only with string collection
    /// as the values of the strings cannot be infered from the IClipboardItem,
    /// and the SearchSupportExtensions are used for that.
    /// </summary>
    public class TextSearchTerm : SearchTermBase<string>
    {
        public TextSearchTerm(IEnumerable<string> textStrings)
            : base(textStrings)
        { }

        protected override SearchCategory SearchCategoryForTerm => SearchCategory.Text;

        protected override IEnumerable<string> GetTermValuesFromClipboardItem(IClipboardItem clipboardItem)
        {
            // we will construct this class only with ready strings, 
            // so we will not need this method
            throw new System.NotImplementedException();
        }
    }
}
