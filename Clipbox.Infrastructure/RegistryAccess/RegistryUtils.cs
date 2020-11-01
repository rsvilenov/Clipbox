using Clipbox.Core;
using Microsoft.Win32;

namespace Clipbox.Infrastructure.RegistryAccess
{
    public static class RegistryUtils
    {
        public static string GetCachedSetupPath()
        {
            string productHKLMRegKey = $@"Software\{ProductConstants.COMPANY_NAME}\{ProductConstants.PRODUCT_NAME}";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(productHKLMRegKey))
            {
                return key?.GetValue("CachedSetupPath")?.ToString();
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
