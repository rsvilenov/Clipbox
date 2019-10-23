using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public abstract class SearchTermBase<T> : SearchObjectBase<T>, ISearchTerm
    {
        public SearchTermBase(IEnumerable<T> searchValues)
        {
            Values = searchValues.ToList();
            StringValues = GetValuesAsStringCollection(Values);
        }

        public TermOccurrence Occurrence { get; set; }
        
        public override string ToString()
        {
            return $"{this.GetType().Name} :  [{ Category }] is in ({string.Join(", ", StringValues ?? Enumerable.Empty<string>() )})";
        }
    }
}
