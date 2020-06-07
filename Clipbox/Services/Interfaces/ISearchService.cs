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
        IEnumerable<string> FindClipboardItemIds(IEnumerable<ISearchTerm> searchTerms);

        /// <summary>
        /// Rebuilds the indeces
        /// </summary>
        void StartIndexRebuilding();

        /// <summary>
        /// Cancels the ongoing process of
        /// rebuilding the indeces.
        /// </summary>
        void CancelRebuildIndices();

        /// <summary>
        /// True if the indeces are healthy and usable.
        /// </summary>
        bool AreIndedisHealthy { get; }
    }
}
