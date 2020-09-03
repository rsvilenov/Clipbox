using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes
{
    public interface ICloudSupport
    {
        CloudServiceInfo GetCloudServiceInfo();

        void CreateCategory(string categoryName);

        void DeleteCategory(string categoryName);

        void PutItem(string categoryName, string itemName, byte[] data);

        void DeleteItem(string categoryName, string itemName);

        byte[] GetItem(string categoryName, string itemName);

        bool CheckCategoryExists(string categoryName);

        bool CheckItemExists(string categoryName, string itemName);

        IEnumerable<string> ListItems(string categoryName, string itemNamePart = "");
    }
}
