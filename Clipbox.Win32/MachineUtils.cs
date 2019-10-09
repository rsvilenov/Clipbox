using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace Clipbox.Win32
{
    public static class MachineUtils
    {
        private static string _machineGuid;

        public static string GetMachineGuid()
        {
            if (_machineGuid != null)
            {
                return _machineGuid;
            }

            // thanks to Derek W's answer here:
            // https://stackoverflow.com/questions/2333149/how-to-fast-get-hardware-id-in-c
            string location = @"SOFTWARE\Microsoft\Cryptography";
            string name = "MachineGuid";


            // this should work on both 32 and 64 operating systems, regardless of the
            // target platform our executable is compiled for.
            // MSDN says this: 
            // "If you request a 64-bit view on a 32-bit operating system, the returned keys will be in the 32-bit view."
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.registryview?view=netframework-4.8
            using (RegistryKey localMachineX64View =
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey rk = localMachineX64View.OpenSubKey(location))
                {
                    if (rk == null)
                    {
                        throw new KeyNotFoundException($"Registry key not found: {location}");
                    }

                    object machineGuid = rk.GetValue(name);
                    if (machineGuid == null)
                    {
                        throw new IndexOutOfRangeException($"Index not found: {name}");
                    }

                    _machineGuid = machineGuid.ToString();

                    return _machineGuid;
                }
            }
        }
    }
}
