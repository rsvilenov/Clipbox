using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Search
{
    public class IndexEntry
    {
        public IndexEntry(string id, IEnumerable<IIndexUnit> indexUnits)
        {
            Id = id;
            IndexUnits = indexUnits.ToList().AsReadOnly();
        }

        public string Id { get; }

        public IReadOnlyCollection<IIndexUnit> IndexUnits { get; }
    }
}
