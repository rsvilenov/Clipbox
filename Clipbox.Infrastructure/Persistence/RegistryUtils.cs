using Clipbox.Core;
using Microsoft.Win32;
using System.IO;

namespace Clipbox.Infrastructure.Persistence
{
    public static class RegistryUtils
    {
        public static string GetInstallationPath(bool useHKCU = false)
        {
            string installPathRegKey = $@"Software\Microsoft\Windows\CurrentVersion\App Paths\{ProductConstants.CLIENT_EXE_NAME}.exe";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(installPathRegKey))
            {
                return key.GetValue(string.Empty)?.ToString();
            }
        }
    }
}
