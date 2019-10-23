using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceWindowCaptionSearchTerm : SearchTermBase<string>
    {
        public SourceWindowCaptionSearchTerm(IEnumerable<string> sourceWindowName)
            : base(sourceWindowName)
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceWindowName;
    }
}
