using Clipbox.Core.Search.Enums;
using System.Collections.Generic;

namespace Clipbox.Core.Search.SearchTerms
{
    public class SourceApplicationNameSearchTerm : SearchTermBase<string>
    {
        public SourceApplicationNameSearchTerm(IEnumerable<string> sourceApplicationNames)
            : base(sourceApplicationNames)
        { }
        
        protected override SearchCategory SearchCategory => SearchCategory.SourceApplicationName;

    }
}
