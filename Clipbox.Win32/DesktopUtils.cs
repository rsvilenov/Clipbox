using System;

namespace Clipbox.Win32
{
    public static class DesktopUtils
    {
        private const UInt32 SPI_GETDESKWALLPAPER = 0x73;
	    private const int MAX_PATH = 260;

        public static string GetWallpaperPath()
        {
            string currentWallpaper = new string('\0', MAX_PATH);
            Native.User32.SystemParametersInfo(SPI_GETDESKWALLPAPER, currentWallpaper.Length, currentWallpaper, 0);
            
            return currentWallpaper.Substring(0, currentWallpaper.IndexOf('\0'));
        }
    }
}
