using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using static Clipbox.Win32.MonitorUtils;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : AcryllicWindow
    {
        public SettingsWindow(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            
            InitializeComponent();

            this.mainGrid.DataContext = this;

            SourceInitialized += SettingsWindow_SourceInitialized;
        }



        public bool IsSettingChanged
        {
            get { return (bool)GetValue(IsSettingChangedProperty); }
            set { SetValue(IsSettingChangedProperty, value); }
        }

        public static readonly DependencyProperty IsSettingChangedProperty =
            DependencyProperty.Register(nameof(IsSettingChanged), typeof(bool), typeof(SettingsWindow), new PropertyMetadata(false, IsSettingChecked_PropertyChanged));

        private static void IsSettingChecked_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SettingsWindow wnd = (SettingsWindow)d;
            if ((bool)e.NewValue)
            {
                wnd._settingsService.SetSetting(SettingsKeys.LicalizationCulture, "bg-BG");
            }
            else
            {
                wnd._settingsService.SetSetting(SettingsKeys.LicalizationCulture, "en-US");
            }
        }

        private void SettingsWindow_SourceInitialized(object sender, EventArgs e)
        {
            DisplayMonitor dm = MonitorUtils.GetByWindowHandle(this.Handle);

            bool resizeToDpi = _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);

            double currentDisplayHeight = dm.WorkingArea.Height;
            double currentDisplayWidth = dm.WorkingArea.Width;

            if (resizeToDpi)
            {
                double scaleFactor = dm.Dpi / 96;
                currentDisplayHeight *= scaleFactor;
                currentDisplayWidth *= scaleFactor;
            }

            this.MaxHeight = currentDisplayHeight - 10;
            this.MaxWidth = currentDisplayWidth - 10;

            var culture = new CultureInfo("bg-BG");
            TranslationSource.Instance.CurrentCulture = culture;
        }

        private ISettingsService _settingsService;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
