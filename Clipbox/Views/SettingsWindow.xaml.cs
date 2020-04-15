using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Windows;
using System.Windows.Input;
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
