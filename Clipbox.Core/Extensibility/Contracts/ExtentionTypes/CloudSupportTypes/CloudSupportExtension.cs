using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes
{
    /// <summary>
    /// Base class for the Cloud Support extensions.
    /// </summary>
    public abstract class CloudSupportExtension :
        ConfigurableExtension,
        ICloudSupport,
        IClipboxExtension
    {
        public abstract void CreateCategory(string categoryName);

        public abstract void DeleteCategory(string categoryName);

        public abstract void PutItem(string categoryName, string itemName, byte[] data);

        public abstract void DeleteItem(string categoryName, string itemName);

        public abstract byte[] GetItem(string categoryName, string itemName);

        public abstract bool CheckCategoryExists(string categoryName);

        public abstract bool CheckItemExists(string categoryName, string itemName);

        public abstract IEnumerable<string> ListItems(string categoryName, string itemNamePart = "");

        public abstract CloudServiceInfo GetCloudServiceInfo();
    }
}
