using Clipbox.Core.Search.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search
{
    public abstract class SearchTermBase<T> : SearchObjectBase<T>, ISearchTerm
    {
        public SearchTermBase(IEnumerable<T> searchValues)
        {
            AddValueRange(searchValues.ToList());
        }

        public SearchTermBase()
        {
        }

        protected SearchTermBase(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public void AddValue(T value)
        {
            AddValueRange(new T[] { value });
        }
        
        public TermOccurrence Occurrence { get; set; }
        
        public override string ToString()
        {
            return $"{this.GetType().Name} :  [{ Category }] is in ({string.Join(", ", StringValues ?? Enumerable.Empty<string>() )})";
        }
    }
}
