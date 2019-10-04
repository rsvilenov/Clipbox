using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public abstract class SearchTermBase<T> : ISearchTerm
    {
        public SearchCategory Category { get; protected set; }

        /// <summary>
        /// The values in that collection are expanded this way: value1 OR value2 OR value3...
        /// </summary>
        public IEnumerable<T> SearchValues { get; private set; }

        protected void SetSearchValues(IEnumerable<T> searchValues)
        {
            SearchValues = searchValues.ToList();
        }

        protected abstract SearchCategory SearchCategoryForTerm {get;}
    }
}
