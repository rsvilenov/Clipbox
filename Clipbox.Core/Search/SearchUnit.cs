using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public class SearchUnit
    {
        public SearchUnit(string id, IEnumerable<ISearchTerm> searchTerms)
        {
            SearchTerms = searchTerms.ToList();
        }

        public string Id { get; }

        public IEnumerable<ISearchTerm> SearchTerms { get; }
    }
}
