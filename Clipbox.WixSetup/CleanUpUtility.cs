using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace Clipbox.WixSetup
{
    public static class CleanUpUtility
    {
        public enum CleanUpType
        {
            RegistryOnly,
            RegistryAndAppData,
            AppDataSettings,
            RegistryAndAppDataSettings
        }

        public static void CleanUp(CleanUpType cleanUpType, bool justCurrentUser = false)
        {
            Dictionary<string, string> sidsAndAppDatas = null;
            if (justCurrentUser)
            {
                var currentUserSid = WindowsIdentity.GetCurrent().User.ToString();
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                sidsAndAppDatas = new Dictionary<string, string>
                {
                    { currentUserSid, appData }
                };
            }
            else
            {
                sidsAndAppDatas = GetAllSidsAndAppDatas();
            }

            foreach (string sid in sidsAndAppDatas.Keys)
            {
                switch (cleanUpType)
                {
                    case CleanUpType.RegistryOnly:
                        DeleteProductHKLUSubKeyForUser(sid);
                        break;
                    case CleanUpType.RegistryAndAppData:
                        DeleteProductHKLUSubKeyForUser(sid);
                        DeleteAppData(sidsAndAppDatas[sid]);
                        break;
                    case CleanUpType.AppDataSettings:
                        DeleteAppDataSettings(sidsAndAppDatas[sid]);
                        break;
                    case CleanUpType.RegistryAndAppDataSettings:
                        DeleteProductHKLUSubKeyForUser(sid);
                        DeleteAppDataSettings(sidsAndAppDatas[sid]);
                        break;
                }
            }

        }

        private static void DeleteAppDataSettings(string appDataPath)
        {
            string appDataSettingsDirPath = Path.Combine(appDataPath, ProductConstants.PRODUCT_NAME, "Settings");
            foreach (string filePath in Directory.GetFiles(appDataSettingsDirPath))
            {
                File.Delete(filePath);
            }
        }

        private static void DeleteAppData(string appDataPath)
        {
            Directory.Delete(Path.Combine(appDataPath, ProductConstants.PRODUCT_NAME), true);
        }

        private static void DeleteProductHKLUSubKeyForUser(string sid)
        {
            using (RegistryKey USERS32 =
                    RegistryKey.OpenBaseKey(RegistryHive.Users,
                        RegistryView.Registry32))
            using (RegistryKey sidKey = USERS32.OpenSubKey(sid, true))
            {
                DeleteOurSubkeysForSid(sidKey);
            }

            using (RegistryKey USERS64 =
                    RegistryKey.OpenBaseKey(RegistryHive.Users,
                        RegistryView.Registry64))
            using (RegistryKey sidKey = USERS64.OpenSubKey(sid, true))
            {
                DeleteOurSubkeysForSid(sidKey);
            }
        }

        private static void DeleteOurSubkeysForSid(RegistryKey sidKey)
        {
            bool areAnyOtherOfOurProducts = false;
            using (var companyKey = sidKey.OpenSubKey($@"Software\{ProductConstants.COMPANY_NAME}", true))
            {
                if (companyKey == null)
                {
                    return;
                }

                areAnyOtherOfOurProducts = companyKey.SubKeyCount > 1;
            }

            if (areAnyOtherOfOurProducts)
            {
                sidKey.DeleteSubKeyTree($@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}");
            }
            else
            {
                sidKey.DeleteSubKeyTree($@"Software\{ProductConstants.COMPANY_NAME}");
            }
        }

        private static Dictionary<string, string> GetAllSidsAndAppDatas()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            using (RegistryKey USERS32 =
                    RegistryKey.OpenBaseKey(RegistryHive.Users,
                        RegistryView.Registry32))
            {
                CollectSidsAndAppDatas(result, USERS32);
            }

            using (RegistryKey USERS64 =
                    RegistryKey.OpenBaseKey(RegistryHive.Users,
                        RegistryView.Registry64))
            {
                CollectSidsAndAppDatas(result, USERS64);
            }

            return result;
        }

        private static void CollectSidsAndAppDatas(Dictionary<string, string> result, RegistryKey USERS32)
        {
            foreach (string subKeyName in USERS32.GetSubKeyNames())
            {
                if (subKeyName.Contains("_Classes") || subKeyName.Equals(".DEFAULT"))
                {
                    continue;
                }

                SecurityIdentifier sid = null;

                try
                {
                    sid = new SecurityIdentifier(subKeyName);
                }
                catch { continue; }

                if (sid != null && sid.IsAccountSid())
                {
                    using (RegistryKey sidKey = USERS32.OpenSubKey(subKeyName))
                    using (RegistryKey subKey = sidKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"))
                    {
                        if (subKey?.GetValue("AppData") is string appDataFolder
                            && !result.ContainsKey(subKeyName))
                        {
                            result.Add(subKeyName, appDataFolder);
                        }
                    }
                }
            }
        }
    }
}
