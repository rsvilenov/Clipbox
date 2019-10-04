using Clipbox.Core.Search;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Search
{
    public interface ISearchProvider
    {
        IEnumerable<string> FindSearchUnitIds(IEnumerable<ISearchTerm> searchTerms);

        void StoreSearchUnits(IEnumerable<SearchUnit> searchUnits);

        void DeleteSearchUnits(IEnumerable<string> searchUnitIds);
    }
}
