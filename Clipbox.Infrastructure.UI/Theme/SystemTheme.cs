using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Theme
{
    public enum ApplicationTheme
    {
        Light,
        Dark,
    }

    // this comes from FluentWPF
    // https://github.com/sourcechord/FluentWPF
    /// <summary>
    /// 
    /// </summary>
    public class SystemTheme : BindableObject, IDisposable
    {
        public Dictionary<string, Color> Colors { get; private set; }

        private HwndSource _hWndSource;

        public event EventHandler ColorSettingsChanged;

        public SystemTheme()
        {
            Colors = new Dictionary<string, Color>();
            Load();


            _hWndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, IntPtr.Zero /* HWND_DESKTOP */);
            _hWndSource.AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((uint)msg)
            {
                case (int)NativeConstants.WmConstants.WM_DWMCOLORIZATIONCOLORCHANGED:
                    // reload the settings from registry
                    ColorSettingsChanged?.Invoke(this, new EventArgs());
                    handled = true;
                    break;
                case (int)NativeConstants.WmConstants.WM_SETTINGCHANGE:
                    var systemParmeter = Marshal.PtrToStringAuto(lParam);
                    if (systemParmeter == "ImmersiveColorSet")
                    {
                        // reload the color scheme
                        ColorSettingsChanged?.Invoke(this, new EventArgs());
                        handled = true;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        internal void Reload()
        {
            Load();
        }

        public Color GetColorByTypeName(string name)
        {
            var colorSet = Win32.Native.UxTheme.GetImmersiveUserColorSetPreference(false, false);
            var colorType = Win32.Native.UxTheme.GetImmersiveColorTypeFromName(name);
            var rawColor = Win32.Native.UxTheme.GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

            var bytes = BitConverter.GetBytes(rawColor);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }

        private ApplicationTheme _theme;
        public ApplicationTheme Theme
        {
            get { return _theme; }
            private set { SetField(ref _theme, value); }
        }

        private bool _acrylicTransparencyEnabled;
        public bool AcrylicTransparencyEnabled
        {
            get { return _acrylicTransparencyEnabled; }
            private set { SetField(ref _acrylicTransparencyEnabled, value); }
        }

        private bool _colorPrevalamceEnabled;
        public bool ColorPrevalanceEnabled
        {
            get { return _colorPrevalamceEnabled; }
            private set { SetField(ref _colorPrevalamceEnabled, value); }
        }

        private void LoadThemeSettings()
        {
            var regkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            
            if (regkey == null)
            {
                Theme = ApplicationTheme.Light;
            }

            var themeValue = regkey.GetValue("AppsUseLightTheme");
            var transparencyValue = regkey.GetValue("EnableTransparency");
            var colorPrevalenceValue = regkey.GetValue("ColorPrevalence");

            // this option was added in windows 10 release 1067,
            // so we cannot count on its existence
            // https://social.technet.microsoft.com/Forums/en-US/aa472dc4-aff1-43d4-bf2b-1fd731bb7da7/missing-options-in-personalization-colors-in-windows-10-enterprise?forum=win10itprogeneral
            Theme = (themeValue != null && (int)themeValue == 0) ? ApplicationTheme.Dark : ApplicationTheme.Light;

            AcrylicTransparencyEnabled = transparencyValue != null && (int)transparencyValue == 1;

            ColorPrevalanceEnabled = colorPrevalenceValue != null && (int)colorPrevalenceValue == 1;
        }

        private void Load()
        {
            Colors.Clear();

            if (OsVersionUtils.CheckIsWin81OrHigher())
            {
                LoadThemeSettings();

                for (UInt32 colorIndex = 0; colorIndex < 0xFFF; colorIndex++)
                {
                    IntPtr typeNamePtr = Win32.Native.UxTheme.GetImmersiveColorNamedTypeByIndex(colorIndex);
                    if (typeNamePtr != IntPtr.Zero)
                    {
                        IntPtr ptrTypeName = (IntPtr)Marshal.PtrToStructure(typeNamePtr, typeof(IntPtr));
                        string typeName = Marshal.PtrToStringUni(ptrTypeName);

                        Color color = GetColorByTypeName("Immersive" + typeName);

                        Colors.Add("Immersive" + typeName, color);
                    }
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Requires Windows 8.1 or higher");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // TODO: threading
                 //_hWndSource.Dispose();

                disposedValue = true;
            }
        }

        ~SystemTheme()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}