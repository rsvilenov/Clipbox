using System.Collections.Generic;
using System.IO;

namespace Clipbox.Infrastructure.Persistence
{
    public class AppDataPersistenceProvider : IPersistenceProvider
    {
        public AppDataPersistenceProvider()
        {
            // create a directory %AppData%\Clipbox if it does not exist.
            AppDataUtils.GetAppDataDir(throwIfNotExists: false);
        }

        public void Append(string key, string data, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.WriteToApplicationData(path, data, append: true);
        }

        public void Write(string key, string data, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.WriteToApplicationData(path, data, append: false);
        }

        public void Write(string key, byte[] data, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.WriteToApplicationData(path, data);
        }
        
        public void Delete(string key, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.DeleteFile(path);
        }

        public void DeleteCategory(string category)
        {
            AppDataUtils.DeleteDirectory(category);
        }

        public IEnumerable<string> ListKeys(string category = null)
        {
            return AppDataUtils.ListFiles(category);
        }
        
        public byte[] ReadBytes(string key, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            return AppDataUtils.ReadBytesFromApplicationData(path);
        }

        public string ReadText(string key, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            return AppDataUtils.ReadTextFromApplicationData(path);
        }

        public IEnumerable<string> ListCategories()
        {
            return AppDataUtils.ListSubDirectories();
        }
        
        public bool CheckIfKeyExists(string key, string category = null)
        {
            string path = GetAppDataRelativeFilePath(key, category);
            return AppDataUtils.CheckFileExists(path);
        }

        public bool CheckIfCategoryExists(string category)
        {
            string path = Path.Combine(AppDataUtils.GetAppDataDir(true), category);
            return Directory.Exists(path);
        }

        private static string GetAppDataRelativeFilePath(string key, string category)
        {
            string path = key;
            if (!string.IsNullOrEmpty(category))
            {
                path = Path.Combine(category, key);
            }

            return path;
        }
    }
}
