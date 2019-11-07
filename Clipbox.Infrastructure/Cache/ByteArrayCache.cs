using Clipbox.Infrastructure.Compression;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Cache
{
    public class ByteArrayCache
    {
        private readonly ConcurrentDictionary<string, CacheObject> _cache;

        private readonly bool _compressData;

        private readonly long _maxSize;

        private long _totalCacheSize;

        public ByteArrayCache(long maxSize, bool compressData = false)
        {
            _cache = new ConcurrentDictionary<string, CacheObject>();
            _maxSize = maxSize;
            _compressData = compressData;
        }

        public void AddOrUpdate(string key, byte[] byteArray)
        {
            byte[] arrayToCache = _compressData ? 
                ByteArrayCompressor.Compress(byteArray) :
                byteArray.ToArray();

            _cache.AddOrUpdate(key, 
                new CacheObject(arrayToCache),
                (k, val) => val);

            _totalCacheSize += arrayToCache.LongLength;

            List<string> keysOldestFirst = _cache
                .OrderBy(co => co.Value.LastAccessedTicks)
                .Select(co => co.Key)
                .ToList();

            foreach(string keyToRemove in keysOldestFirst)
            {
                if (_cache.TryRemove(keyToRemove, out CacheObject obj))
                {
                    _totalCacheSize -= obj.ByteArray.LongLength;
                }

                if (_totalCacheSize < _maxSize)
                {
                    break;
                }
            }
        }

        public bool TryGet(string key, out byte[] byteArray)
        {
            bool success = _cache.TryGetValue(key, out CacheObject obj);

            if (success)
            {
                obj.UpdateLastAccessTicks();
            }

            byteArray = _compressData ?
                ByteArrayCompressor.Decompress(obj.ByteArray) :
                obj.ByteArray.ToArray();
            

            return success;
        }

        private class CacheObject
        {
            public CacheObject(byte[] byteArray)
            {
                ByteArray = byteArray;
                LastAccessedTicks = DateTime.Now.Ticks;
            }

            public void UpdateLastAccessTicks()
            {
                LastAccessedTicks = DateTime.Now.Ticks;
            }

            public byte[] ByteArray { get; }

            public long LastAccessedTicks { get; private set; }
        }
    }
}
