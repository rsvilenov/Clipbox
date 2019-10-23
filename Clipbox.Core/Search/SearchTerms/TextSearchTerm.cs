using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class TextSearchTerm : SearchTermBase<string>
    {
        public TextSearchTerm(IEnumerable<string> textStrings)
            : base(textStrings)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.Text;
    }
}
