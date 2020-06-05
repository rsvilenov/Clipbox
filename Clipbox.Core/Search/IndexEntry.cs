using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Clipbox.Core.Search
{
    [Serializable]
    public class IndexEntry : ISerializable
    {
        public IndexEntry(string id, IEnumerable<IIndexUnit> indexUnits)
        {
            Id = id;
            IndexUnits = indexUnits.ToList().AsReadOnly();
        }

        private IndexEntry(SerializationInfo info, StreamingContext context)
        {
            Id = (string)info.GetValue("id", typeof(string));

            List<IIndexUnit> indexUnits = (List<IIndexUnit>)info.GetValue("indexUnits", typeof(List<IIndexUnit>));
            IndexUnits = indexUnits.AsReadOnly();
        }

        public string Id { get; }

        public IReadOnlyCollection<IIndexUnit> IndexUnits { get; }
        
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("id", Id, typeof(string));
            info.AddValue("indexUnits", new List<IIndexUnit>(IndexUnits), typeof(List<IIndexUnit>));
        }
    }
}
