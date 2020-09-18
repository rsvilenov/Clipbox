using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Cache
{
    public class CloudObjectCache
    {
        private readonly List<CloudObjectInfo> _cloudObjects;
        private readonly object _cloudObjectsLockHandle;

        public CloudObjectCache()
        {
            _cloudObjects = new List<CloudObjectInfo>();
            _cloudObjectsLockHandle = new object();
        }

        public void AddItem(string categoryName, string itemName, string itemId = "")
        {
            lock (_cloudObjectsLockHandle)
            {
                if (CheckItemExistsNoLock(categoryName, itemName))
                {
                    throw new ArgumentException($"{categoryName}/{itemName} already exists");
                }

                _cloudObjects.Add(new CloudObjectInfo
                {
                    Id = itemId,
                    ParentName = categoryName,
                    IsFolder = false,
                    Name = itemName
                });
            }
        }

        public void AddCategory(string categoryName, string categoryId = "")
        {
            lock (_cloudObjectsLockHandle)
            {
                if (CheckCategoryExistsNoLock(categoryName))
                {
                    throw new ArgumentException($"{categoryName} already exists");
                }

                _cloudObjects.Add(new CloudObjectInfo
                {
                    Id = categoryId,
                    IsFolder = true,
                    Name = categoryName
                });
            }
        }

        public bool CheckCategoryExists(string categoryName)
        {
            lock (_cloudObjectsLockHandle)
            {
                return CheckCategoryExistsNoLock(categoryName);
            }
        }

        private bool CheckCategoryExistsNoLock(string categoryName)
        {
            return _cloudObjects.Any(co => true == co.Name?.Equals(categoryName, StringComparison.OrdinalIgnoreCase)
                && co.IsFolder);
        }

        public bool CheckItemExists(string categoryName, string itemName)
        {
            lock (_cloudObjectsLockHandle)
            {
                return CheckItemExistsNoLock(categoryName, itemName);
            }
        }

        private bool CheckItemExistsNoLock(string categoryName, string itemName)
        {
            return _cloudObjects.Any(co => true == co.Name?.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                    && true == co.ParentName?.Equals(categoryName, StringComparison.OrdinalIgnoreCase)
                    && !co.IsFolder);
        }

        public ICloudObjectInfo GetItemInfo(string categoryName, string itemName)
        {
            lock (_cloudObjectsLockHandle)
            {
                return GetItemInfoNoLock(categoryName, itemName);
            }
        }

        private CloudObjectInfo GetItemInfoNoLock(string categoryName, string itemName)
        {
            return _cloudObjects.FirstOrDefault(co => true == co.Name?.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                && true == co.ParentName?.Equals(categoryName, StringComparison.OrdinalIgnoreCase)
                && !co.IsFolder);
        }

        public ICloudObjectInfo GetCategoryInfo(string categoryName)
        {
            lock (_cloudObjectsLockHandle)
            {
                return GetCategoryInfoNoLock(categoryName);
            }
        }

        private CloudObjectInfo GetCategoryInfoNoLock(string categoryName)
        {
            return _cloudObjects.FirstOrDefault(co => true == co.Name?.Equals(categoryName, StringComparison.OrdinalIgnoreCase)
                && co.IsFolder);
        }

        public void RemoveCategory(string categoryName)
        {
            lock (_cloudObjectsLockHandle)
            {
                CloudObjectInfo coiToRemove = GetCategoryInfoNoLock(categoryName);
                if (coiToRemove != null)
                {
                    _cloudObjects.Remove(coiToRemove);
                }
            }
        }

        public void RemoveItem(string categoryName, string itemName)
        {
            lock (_cloudObjectsLockHandle)
            {
                CloudObjectInfo coiToRemove = GetItemInfoNoLock(categoryName, itemName);
                if (coiToRemove != null)
                {
                    _cloudObjects.Remove(coiToRemove);
                }
            }
        }

        public IReadOnlyCollection<ICloudObjectInfo> ListCloudObjectInfo()
        {
            lock (_cloudObjectsLockHandle)
            {
                return _cloudObjects.AsReadOnly();
            }
        }

        private class CloudObjectInfo : ICloudObjectInfo
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public bool IsFolder { get; set; }

            public string ParentName { get; set; }
        }
    }
}
