using Clipbox.Core;
using Clipbox.Infrastructure.Encryption;
using System;
using System.IO;

namespace Clipbox.Infrastructure.Persistence
{
    public static class AppDataUtils
    {
        private const int FILE_LOCK_TIMEOUT_MS = 1000;

        public static void WriteToApplicationData(string relativeFilePath, string data, bool append = false, bool useLock = true)
        {
            EnsurePathIsRelative(relativeFilePath);

            string dir = GetAppDataDir(throwIfNotExists: false);
            string fullPath = Path.Combine(dir, relativeFilePath);
            string fullDirName = Path.GetDirectoryName(fullPath);

            if (fullDirName != dir && !Directory.Exists(fullDirName))
            {
                Directory.CreateDirectory(fullDirName);
            }

            DoFileOperation(() =>
            {
                using (StreamWriter sw = new StreamWriter(fullPath, append))
                {
                    sw.Write(data);
                }
            }, fullPath, useLock);
            
        }

        public static string ReadTextFromApplicationData(string relativeFilePath, bool useLock = true)
        {
            EnsurePathIsRelative(relativeFilePath);

            string dir = GetAppDataDir(throwIfNotExists: true);
            string fullPath = Path.Combine(dir, relativeFilePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"{relativeFilePath} not found in {fullPath}. Please use WriteToApplicationData() first.");
            }

            string text = null;
            DoFileOperation(() =>
            {
                text = File.ReadAllText(fullPath);
            }, fullPath, useLock);

            return text;
        }

        public static void WriteToApplicationData(string relativeFilePath, byte[] data, bool useLock = true)
        {
            EnsurePathIsRelative(relativeFilePath);

            string dir = GetAppDataDir(throwIfNotExists: false);
            string fullPath = Path.Combine(dir, relativeFilePath);

            DoFileOperation(() =>
            {
                File.WriteAllBytes(fullPath, data);
            }, fullPath, useLock);
        }

        public static byte[] ReadBytesFromApplicationData(string relativeFilePath, bool useLock = true)
        {
            EnsurePathIsRelative(relativeFilePath);

            string dir = GetAppDataDir(throwIfNotExists: true);

            string fullPath = Path.Combine(dir, relativeFilePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"{relativeFilePath} not found in {fullPath}. Please use WriteToApplicationData() first.");
            }

            byte[] contents = null;
            DoFileOperation(() =>
            {
                contents = File.ReadAllBytes(fullPath);
            }, fullPath, useLock);

            return contents;
        }
        
        private static void EnsurePathIsRelative(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                throw new InvalidOperationException($@"This method works only with paths, relative to %appdata%\{ProductConstants.PRODUCT_NAME}.");
            }
        }

        public static bool CheckFileExists(string relativeFilePath)
        {
            string fullFilePath = Path.Combine(GetAppDataDir(throwIfNotExists: true), relativeFilePath);
            return File.Exists(fullFilePath);
        }

        public static string GetAppDataDir(bool throwIfNotExists)
        {
            string mainAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appSubdir = Path.Combine(mainAppDataDir, ProductConstants.PRODUCT_NAME);
            if (!Directory.Exists(appSubdir))
            {
                if (throwIfNotExists)
                {
                    throw new DirectoryNotFoundException($"{ProductConstants.PRODUCT_NAME} dirctory not found in AppData. Please use WriteToApplicationData() first.");
                }
                else
                {
                    Directory.CreateDirectory(appSubdir);
                }
            }
            return appSubdir;
        }

        private static void DoFileOperation(Action operation, string filePath, bool useLock)
        {
            if (useLock)
            {
                using (GlobalNamedMutex gMutex = new GlobalNamedMutex($"{MD5Hash.Create(filePath)}", FILE_LOCK_TIMEOUT_MS))
                {
                    operation();
                }
            }
            else
            {
                operation();
            }
        }

    }
}
