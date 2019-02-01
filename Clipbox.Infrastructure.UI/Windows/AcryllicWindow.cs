using Clipbox.Infrastructure.UI.Theme;
using Clipbox.Win32;
using System.Runtime.InteropServices;

namespace Clipbox.Infrastructure.UI.Windows
{
    public class AcryllicWindow : PerMonitorDpiWindow
    {
        private bool _isBlurEnabled;
        /// <summary>
        /// Enables blur effects and listening to changes in ThemeManager.TransparencySettingChanged.
        /// If the transparency settings do not allow blur, this property has effect only after the blur is enabled by the system settings.
        /// </summary>
        public bool IsBlurEnabled
        {
            get { return _isBlurEnabled; }
            set
            {
                _isBlurEnabled = value;
                SetBlurPreference(_isBlurEnabled);
            }
        }

        public AcryllicWindow()
        {
            SourceInitialized += AcryllicWindow_SourceInitialized;
            Closing += AcryllicWindow_Closing;
        }

        private void AcryllicWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ThemeManager.TransparencySettingChanged -= ThemeManager_TransparencySettingChanged;
        }

        private void AcryllicWindow_SourceInitialized(object sender, System.EventArgs e)
        {
            if (IsBlurEnabled)
            {
                SetBlurPreference(ThemeManager.IsAcryllicTransparencyEnabled);
            }

            ThemeManager.TransparencySettingChanged += ThemeManager_TransparencySettingChanged;
        }

        private void ThemeManager_TransparencySettingChanged(object sender, System.EventArgs e)
        {
            SetBlurPreference(ThemeManager.IsAcryllicTransparencyEnabled && IsBlurEnabled);
        }

        private void SetBlurPreference(bool enableBlur)
        {
            var accent = new Win32.Native.NativeStructures.AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);

            if (enableBlur)
            {
                accent.AccentState = Win32.OsVersionUtils.CheckIsWin81OrHigher() ? Win32.Native.NativeEnums.AccentState.ACCENT_ENABLE_BLURBEHIND : Win32.Native.NativeEnums.AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
            }
            else
            {
                accent.AccentState = Win32.Native.NativeEnums.AccentState.ACCENT_DISABLED;
            }

            accent.AccentFlags = 2;
            accent.GradientColor = 0x00FFFFFF;

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new Win32.Native.NativeStructures.WindowCompositionAttributeData();
                data.Attribute = Win32.Native.NativeEnums.WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                int retCode = Win32.Native.User32.SetWindowCompositionAttribute(this.Handle, ref data);
                if (retCode != 0)
                {
                    MarshalExt.ThrowOnWin32Error();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
    }
}
