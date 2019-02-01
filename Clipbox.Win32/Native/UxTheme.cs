using System;
using System.Runtime.InteropServices;

namespace Clipbox.Win32.Native
{
    public static class UxTheme
    {
        [DllImport(ExternDll.UxTheme, EntryPoint = "#95", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);
        [DllImport(ExternDll.UxTheme, EntryPoint = "#96", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorTypeFromName(string name);
        [DllImport(ExternDll.UxTheme, EntryPoint = "#98", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        [DllImport(ExternDll.UxTheme, EntryPoint = "#100", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetImmersiveColorNamedTypeByIndex(uint dwIndex);
    }
}
