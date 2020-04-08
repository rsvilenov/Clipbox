using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceWindowCaptionSearchTerm : SearchTermBase<string>
    {
        public SourceWindowCaptionSearchTerm(IEnumerable<string> sourceWindowCaptionPart)
            : base(sourceWindowCaptionPart)
        { }

        public SourceWindowCaptionSearchTerm()
            : base()
        { }

        protected override SearchCategory SearchCategory => SearchCategory.SourceWindowName;
    }
}
