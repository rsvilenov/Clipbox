using System.Collections.Generic;

namespace Clipbox.Infrastructure.Persistence
{
    public interface IPersistenceProvider
    {
        void Write(string key, string data, string category = null);
        void Write(string key, byte[] data, string category = null);
        void Append(string key, string data, string category = null);
        byte[] ReadBytes(string key, string category = null);
        string ReadText(string key, string category = null);
        void DeleteCategory(string category);
        void CreateCategory(string category);
        void Delete(string key, string category = null);
        bool CheckIfKeyExists(string key, string category = null);
        bool CheckIfCategoryExists(string category);
        IEnumerable<string> ListKeys(string category = null);
        IEnumerable<string> ListCategories();
    }
}
