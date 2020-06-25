using Microsoft.Win32;

namespace Clipbox.Win32
{
    public static class CpuUtils
    {
        public static string GetCpuName()
        {
            using (RegistryKey cpuNameKey
                = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree))
            {
                    return cpuNameKey?.GetValue("ProcessorNameString") as string;
            }
        }
    }
}
