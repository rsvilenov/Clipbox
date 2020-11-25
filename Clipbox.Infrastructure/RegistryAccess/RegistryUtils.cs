using Clipbox.Core;
using Microsoft.Win32;

namespace Clipbox.Infrastructure.RegistryAccess
{
    public static class RegistryUtils
    {
        public static string GetBundleCachePath()
        {
            string bundleUpgradeCode = null;
            string productHKLMRegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(productHKLMRegKey))
            {
                bundleUpgradeCode = key?.GetValue("BundleUpgradeCode")?.ToString();
            }

            string bundleCachePath = null;
            if (!string.IsNullOrEmpty(bundleUpgradeCode))
            {
                string uninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

                // by defailt Wix stores the bundle cache path in the WOW6432Node subkey, for both 32 and 64 bit installs
                using (RegistryKey HKLM32 =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        RegistryView.Registry32))
                using (RegistryKey key = HKLM32.OpenSubKey(uninstallRegKey))
                {
                    bundleCachePath = SearchSubKeysForBundleCachePath(bundleUpgradeCode, key);
                }

                // but the abovementioned behavior may change in the future...
                // this would most probably require changing this code, but for any case, let's chack in HKLM64
                if (bundleCachePath == null)
                {
                    using (RegistryKey HKLM64 =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        RegistryView.Registry64)) // if the OS is 32 bit, this will redirect us to the regular (32 bit) HKLM
                    using (RegistryKey key = HKLM64.OpenSubKey(uninstallRegKey))
                    {
                        bundleCachePath = SearchSubKeysForBundleCachePath(bundleUpgradeCode, key);
                    }
                }
            }

            return bundleCachePath;
        }

        private static string SearchSubKeysForBundleCachePath(string bundleUpgradeCode, RegistryKey key)
        {
            string bundleCachePath = null;

            foreach (string subKeyName in key.GetSubKeyNames())
            {
                using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                {
                    var upgradeCodeValue = subKey.GetValue("BundleUpgradeCode");

                    if (upgradeCodeValue != null
                        && upgradeCodeValue is string[] upgradeCodeArray
                        && upgradeCodeArray.Length > 0
                        && upgradeCodeArray[0] == bundleUpgradeCode)
                    {
                        bundleCachePath = subKey?.GetValue("BundleCachePath")?.ToString();
                    }
                }
            }

            return bundleCachePath;
        }

        public static string GetProductVersion()
        {
            string productHKLMRegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(productHKLMRegKey))
            {
                return key?.GetValue("ProductVersion")?.ToString();
            }
        }

        public static string GetInitialCultureCode()
        {
            string productHKLMRegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(productHKLMRegKey))
            {
                return key?.GetValue("InitialCultureCode")?.ToString();
            }
        }

        public static string GetCultureCode()
        {
            string productHKCURegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(productHKCURegKey))
            {
                return key?.GetValue("CultureCode")?.ToString();
            }
        }

        public static string GetInitialPinTrayIcon()
        {
            string productHKLMRegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(productHKLMRegKey))
            {
                return key?.GetValue("InitialPinTrayIcon")?.ToString();
            }
        }

        public static void StoreCultureCode(string cultureCode)
        {
            string productHKCURegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";

            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.OpenSubKey(productHKCURegKey, true);
                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(productHKCURegKey, true);
                }
                key?.SetValue("CultureCode", cultureCode);
            }
            finally
            {
                key?.Dispose();
            }
        }
    }
}
