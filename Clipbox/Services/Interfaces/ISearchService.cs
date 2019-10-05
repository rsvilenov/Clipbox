using Clipbox.Core.Search;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface ISearchService
    {
        /// <summary>
        /// Find by a list of search terms. The terms are expanded like this: term1 AND term2 AND term3...
        /// </summary>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        IEnumerable<string> FindSearchUnitIds(IEnumerable<ISearchTerm> searchTerms);

        void StoreSearchUnit(SearchUnit searchUnit);

        void DeleteSearchUnit(string unitId);
    }
}
