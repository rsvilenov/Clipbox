using Clipbox.Core.Search;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Search
{
    public interface ISearchProvider
    {
        IEnumerable<string> FindIndexEntryIds(IEnumerable<ISearchTerm> searchTerms);

        void StoreIndexEntries(IEnumerable<IndexEntry> indexEntries);

        void DeleteIndexEntries(IEnumerable<string> indexEntryIds);
    }
}
