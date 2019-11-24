using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.Infrastructure.CLR
{
    public abstract class TrackableObject
    {
        private static ConcurrentDictionary<int, ObjectInfo> _livingObjects { get; set; } = new ConcurrentDictionary<int, ObjectInfo>();

        private int _hashCode;

        public TrackableObject()
        {
#if DEBUG
            _hashCode = GetHashCode();

            // TryAdd should not fail unless the algorithm in GetHashCode() produces collisions
            _livingObjects.TryAdd(_hashCode, new ObjectInfo
            {
                HashCode = _hashCode,
                AllocationStackTrace = new StackTrace().ToString(),
                CreateDate = DateTime.Now
            });

#endif
        }

        ~TrackableObject()
        {
#if DEBUG
            Debug.WriteLine($"Alive objects: {_livingObjects.Count} - {string.Join(",", _livingObjects.Select(obj => $"{obj.Key} : {obj.Value.CreateDate.ToString()} {Environment.NewLine}").ToList())}");

            _livingObjects.TryRemove(_hashCode, out ObjectInfo objInfo);
#endif
        }

        private class ObjectInfo
        {
            public int HashCode { get; set; }
            
            public DateTime CreateDate { get; set; }

            public string AllocationStackTrace { get; set; }
        }
    }
}
