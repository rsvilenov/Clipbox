using Clipbox.Infrastructure.Persistence.FilesystemAccess;
using System;
using System.Collections.Generic;
using System.IO;

namespace Clipbox.Infrastructure.Persistence.PersistenceProviders
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
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.WriteToApplicationData(path, data, append: true);
        }

        public void Write(string key, string data, string category = null)
        {
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.WriteToApplicationData(path, data, append: false);
        }

        public void Write(string key, byte[] data, string category = null)
        {
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.WriteToApplicationData(path, data);
        }
        
        public void Delete(string key, string category = null)
        {
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            AppDataUtils.DeleteFile(path);
        }

        public void DeleteCategory(string category)
        {
            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            AppDataUtils.DeleteDirectory(category);
        }

        public IEnumerable<string> ListKeys(string category = null)
        {
            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            return AppDataUtils.ListFiles(category);
        }
        
        public byte[] ReadBytes(string key, string category = null)
        {
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            return AppDataUtils.ReadBytesFromApplicationData(path);
        }

        public string ReadText(string key, string category = null)
        {
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            return AppDataUtils.ReadTextFromApplicationData(path);
        }

        public IEnumerable<string> ListCategories()
        {
            return AppDataUtils.ListSubDirectories();
        }
        
        public bool CheckIfKeyExists(string key, string category = null)
        {
            ValidateKey(key);

            if (!string.IsNullOrEmpty(category))
            {
                ValidateCategory(category);
            }

            string path = GetAppDataRelativeFilePath(key, category);
            return AppDataUtils.CheckFileExists(path);
        }

        public bool CheckIfCategoryExists(string category)
        {
            ValidateCategory(category);
            return AppDataUtils.CheckSubDirExists(category);
        }

        public void CreateCategory(string category)
        {
            ValidateCategory(category);
            AppDataUtils.CreateSubDir(category);
        }

        private static string GetAppDataRelativeFilePath(string key, string category)
        {
            string path = key;
            if (!string.IsNullOrWhiteSpace(category))
            {
                path = Path.Combine(category, key);
            }

            return path;
        }

        private void ValidateCategory(string category)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            if (category.IndexOfAny(invalidChars) > -1)
            {
                throw new ArgumentException($"The category contains some of the following invalid chars: {string.Join(",", invalidChars)}");
            }
        }

        private void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key is empty");
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (key.IndexOfAny(invalidChars) > -1)
            {
                throw new ArgumentException($"The key contains some of the following invalid chars: {string.Join(",", invalidChars)}");
            }
        }
    }
}
