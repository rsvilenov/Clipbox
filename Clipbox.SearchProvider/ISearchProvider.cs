using Clipbox.Core.Search;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Search
{
    /// <summary>
    /// A search provider.
    /// </summary>
    public interface ISearchProvider
    {
        /// <summary>
        /// Allows searching for entries by a collectin of entry ids.
        /// </summary>
        /// <param name="searchTerms">A collection of search terms..</param>
        /// <returns>A collection of matching entry ids</returns>
        IEnumerable<string> FindIndexEntryIds(IEnumerable<ISearchTerm> searchTerms);

        /// <summary>
        /// Stores new index entries.
        /// </summary>
        /// <param name="indexEntries">A collection of new index entries to store.</param>
        void StoreIndexEntries(IEnumerable<IndexEntry> indexEntries);

        /// <summary>
        /// Deletes index entries by entry id
        /// </summary>
        /// <param name="indexEntryIds">A collection of entry ids to delete.</param>
        void DeleteIndexEntries(IEnumerable<string> indexEntryIds);

        /// <summary>
        /// Deletes all index entries.
        /// </summary>
        void ClearIndexEntries();

        /// <summary>
        /// Gets the health state of the index.
        /// </summary>
        /// <returns></returns>
        IndexHealthInfo GetIndexHealth();
    }
}
