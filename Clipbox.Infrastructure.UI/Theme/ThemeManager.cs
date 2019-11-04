using Clipbox.Infrastructure.UI.Theme.Enums;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Clipbox.Infrastructure.UI.Theme
{
    public static class ThemeManager
    {
        private static ResourceDictionary _currentTheme;

        private static SystemTheme _systemTheme;

        public static event EventHandler TransparencySettingChanged;

        static ThemeManager()
        {
            _currentTheme = new ResourceDictionary();
            _systemTheme = new SystemTheme();

            _systemTheme.PropertyChanged += (s, a) =>
            {
                if (a.PropertyName == nameof(_systemTheme.AcrylicTransparencyEnabled))
                {
                    TransparencySettingChanged?.Invoke(null, new EventArgs());
                }
            };

            _systemTheme.ColorSettingsChanged += (s, a) => LoadThemeFromSystemTheme();

            LoadDefaultStyle();
        }

        public static bool IsAcryllicTransparencyEnabled
        {
            get
            {
                return _systemTheme.AcrylicTransparencyEnabled;
            }
        }

        public static bool IsLoaded { get; private set; }

        public static void LoadThemeFromSystemTheme()
        {
            const string DEFAULT_ACTIONCENTER_BACKGROUND = "#FF262626";

            _systemTheme.Reload();
            var newTheme = new ResourceDictionary();

            Color systemAccent = FindSystemColor("ImmersiveSystemAccent") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentColor.ToString(), systemAccent);
            newTheme.Add(ThemeBrush.AccentBrush.ToString(), CreateBrush(systemAccent));

            Color systemAccentDark1 = FindSystemColor("ImmersiveSystemAccentDark1") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentDark1Color.ToString(), systemAccentDark1);
            newTheme.Add(ThemeBrush.AccentDark1Brush.ToString(), CreateBrush(systemAccentDark1));

            Color systemAccentDark2 = FindSystemColor("ImmersiveSystemAccentDark2") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentDark2Color.ToString(), systemAccentDark2);
            newTheme.Add(ThemeBrush.AccentDark2Brush.ToString(), CreateBrush(systemAccentDark2));

            Color systemAccentDark3 = FindSystemColor("ImmersiveSystemAccentDark3") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentDark3Color.ToString(), systemAccentDark3);
            newTheme.Add(ThemeBrush.AccentDark3Brush.ToString(), CreateBrush(systemAccentDark3));

            Color systemAccentLight1 = FindSystemColor("ImmersiveSystemAccentLight1") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentLight1Color.ToString(), systemAccentLight1);
            newTheme.Add(ThemeBrush.AccentLight1Brush.ToString(), CreateBrush(systemAccentLight1));

            Color systemAccentLight2 = FindSystemColor("ImmersiveSystemAccentLight2") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentLight2Color.ToString(), systemAccentLight2);
            newTheme.Add(ThemeBrush.AccentLight2Brush.ToString(), CreateBrush(systemAccentLight2));

            Color systemAccentLight3 = FindSystemColor("ImmersiveSystemAccentLight3") ?? Colors.AliceBlue;
            newTheme.Add(ThemeColor.AccentLight3Color.ToString(), systemAccentLight3);
            newTheme.Add(ThemeBrush.AccentLight3Brush.ToString(), CreateBrush(systemAccentLight3));

            string taskbarColorKey = "ImmersiveDarkChromeTaskbarBase";
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                taskbarColorKey = "ImmersiveSystemAccentDark2";
            }
            Color taskbarColor = FindSystemColor(taskbarColorKey) ?? Colors.Red;
            newTheme.Add(ThemeColor.TaskbarColor.ToString(), taskbarColor);
            newTheme.Add(ThemeBrush.TaskbarColorBrush.ToString(), CreateBrush(taskbarColor));

            Color actionCenterColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                actionCenterColor = FindSystemColor("ImmersiveSystemAccentDark1") ?? Colors.Red;
            }
            else
            {
                actionCenterColor = (Color)ColorConverter.ConvertFromString(DEFAULT_ACTIONCENTER_BACKGROUND);
            }
            newTheme.Add(ThemeColor.ActionCenterColor.ToString(), actionCenterColor);
            newTheme.Add(ThemeBrush.ActionCenterBrush.ToString(), CreateBrush(actionCenterColor));

            Color controlBackgroundColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlBackgroundColor = FindSystemColor("ImmersiveSystemAccent") ?? Colors.Red;
            }
            else
            {
                controlBackgroundColor = (Color)ColorConverter.ConvertFromString("#2b2b2b"); //("#4C4C4C");
            }
            newTheme.Add(ThemeColor.ControlBackgroundColor.ToString(), controlBackgroundColor);
            newTheme.Add(ThemeBrush.ControlBackgroundBrush.ToString(), CreateBrush(controlBackgroundColor));

            Color controlHighlightBackgroundColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlHighlightBackgroundColor = (Color)ColorConverter.ConvertFromString("#414141");//("#4C4C4C");
            }
            else
            {
                controlHighlightBackgroundColor = (Color)ColorConverter.ConvertFromString("#414141");//("#4C4C4C");
            }
            newTheme.Add(ThemeColor.ControlHighlightBackgroundColor.ToString(), controlHighlightBackgroundColor);
            newTheme.Add(ThemeBrush.ControlHighlightBackgroundBrush.ToString(), CreateBrush(controlHighlightBackgroundColor));

            Color controlForegroundColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            }
            else
            {
                controlForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            }
            newTheme.Add(ThemeColor.ControlForegroundColor.ToString(), controlForegroundColor);
            newTheme.Add(ThemeBrush.ControlForegroundBrush.ToString(), CreateBrush(controlForegroundColor));

            Color controlHighlightForegroundColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlHighlightForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            }
            else
            {
                controlHighlightForegroundColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            }
            newTheme.Add(ThemeColor.ControlHighlightForegroundColor.ToString(), controlHighlightForegroundColor);
            newTheme.Add(ThemeBrush.ControlHighlightForegroundBrush.ToString(), CreateBrush(controlHighlightForegroundColor));

            Color controlBorderColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlBorderColor = (Color)ColorConverter.ConvertFromString("#808080");// FindSystemColor("ImmersiveSystemAccent") ?? Colors.Red;
            }
            else
            {
                controlBorderColor = (Color)ColorConverter.ConvertFromString("#808080");
            }
            newTheme.Add(ThemeColor.ControlBorderColor.ToString(), controlBorderColor);
            newTheme.Add(ThemeBrush.ControlBorderBrush.ToString(), CreateBrush(controlBorderColor));

            Color controlHighlightBorderColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlHighlightBorderColor = (Color)ColorConverter.ConvertFromString("#949494");
            }
            else
            {
                controlHighlightBorderColor = (Color)ColorConverter.ConvertFromString("#949494");
            }
            newTheme.Add(ThemeColor.ControlHighlightBorderColor.ToString(), controlHighlightBorderColor);
            newTheme.Add(ThemeBrush.ControlHighlightBorderBrush.ToString(), CreateBrush(controlHighlightBorderColor));

            Color controlPressedColor = Colors.Gray;
            if (_systemTheme.ColorPrevalanceEnabled)
            {
                controlPressedColor = (Color)ColorConverter.ConvertFromString("#949494");
            }
            else
            {
                controlPressedColor = (Color)ColorConverter.ConvertFromString("#949494");
            }
            newTheme.Add(ThemeColor.ControlPressedColor.ToString(), controlPressedColor);
            newTheme.Add(ThemeBrush.ControlPressedBrush.ToString(), CreateBrush(controlPressedColor));

            Color? applicationBackgroundColor = null;
            if (_systemTheme.Theme == ApplicationTheme.Dark)
            {
                applicationBackgroundColor = FindSystemColor("ImmersiveApplicationBackgroundDarkTheme");
            }
            else
            {
                applicationBackgroundColor = FindSystemColor("ImmersiveApplicationBackgroundLightTheme");
            }

            if (applicationBackgroundColor == null)
            {
                applicationBackgroundColor = FindSystemColor("ImmersiveApplicationBackground") ?? Colors.Red;
            }

            newTheme.Add(ThemeColor.ApplicationBackgroundColor.ToString(), applicationBackgroundColor.Value);
            newTheme.Add(ThemeBrush.ApplicationBackgroundBrush.ToString(), CreateBrush(applicationBackgroundColor.Value));

            // TODO: remove the hardcoded color
            var alertColor1 = (Color)ColorConverter.ConvertFromString("#FFBF1321");
            newTheme.Add(ThemeColor.AlertColor1.ToString(), alertColor1);
            newTheme.Add(ThemeBrush.AlertBrush1.ToString(), CreateBrush(alertColor1));

            var alertColor2 = (Color)ColorConverter.ConvertFromString("#FFE81123");
            newTheme.Add(ThemeColor.AlertColor2.ToString(), alertColor2);
            newTheme.Add(ThemeBrush.AlertBrush2.ToString(), CreateBrush(alertColor2));

            if (Application.Current.Resources.MergedDictionaries.Contains(_currentTheme))
            {
                Application.Current.Resources.MergedDictionaries.Remove(_currentTheme);
            }
            Application.Current.Resources.MergedDictionaries.Add(newTheme);
            _currentTheme = newTheme;

            IsLoaded = true;
        }

        private static Color? FindSystemColor(string colorName)
        {
            if (_systemTheme.Colors.
                Any(c => c.Key == colorName))
            {
                return _systemTheme.Colors.
                    Where(c => c.Key == colorName).
                    Select(c => c.Value).First();
            }

            return null;
        }

        private static Brush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static void LoadDefaultStyle()
        {
            var defaultStyleDictionary = new ResourceDictionary();
            defaultStyleDictionary.Source =
                new Uri("/Clipbox.Infrastructure.UI;component/Resources/Styles/Style.xaml",
                        UriKind.RelativeOrAbsolute);
            Application.Current.Resources.MergedDictionaries.Add(defaultStyleDictionary);
        }

    }
}
