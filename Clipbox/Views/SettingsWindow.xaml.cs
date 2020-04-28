using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public SettingsWindow(ISettingsService settingsService, SettingsUIExtensionContainer extContainer)
        {
            _settingsService = settingsService;
            _extContainer = extContainer;

            InitializeComponent();

            this.mainGrid.DataContext = this;

            SourceInitialized += SettingsWindow_SourceInitialized;
            Loaded += SettingsWindow_Loaded;
            Closing += SettingsWindow_Closing;
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (ISettingChangeRequestor ext in _settingUIExtensions
                .Where(ext => ext is ISettingChangeRequestor))
            {
                ext.RequestSettingChange -= SettingChangeRequestor_RequestSettingChange;
            }
        }

        private List<IConfigurationRecipient> _settingUIExtensions = new List<IConfigurationRecipient>();
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IEnumerable<SettingsUIExtension> extensions = _extContainer.ResolveAll();
            foreach (var ext in extensions)
            {
                IEnumerable<ISettingKey> allRegisteredKeys = ((ISettingsRegistrator)ext).GetSettingKeys();
                foreach (ISettingKey key in allRegisteredKeys)
                {
                    var configurationRecipient = (IConfigurationRecipient)ext;
                    configurationRecipient.PutSetting(key, _settingsService.GetSetting(key));
                    _settingUIExtensions.Add(configurationRecipient);
                    var settingChangeRequestor = (ISettingChangeRequestor)ext;
                    settingChangeRequestor.RequestSettingChange += SettingChangeRequestor_RequestSettingChange;
                }

                UIElement extUI = ext.GetSettingsViewPart(200);
                pluginContainer.Children.Add(extUI);
            }
        }

        private void SettingChangeRequestor_RequestSettingChange(object sender, Core.Extensibility.EventArgsClasses.RequestSettingChangeEventArgs e)
        {
            ((IConfigurationRecipient)sender).PutSetting(e.SettingKey, e.Value);
            _settingsService.SetSetting(e.SettingKey, e.Value);
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
                wnd._settingsService.SetSetting(SettingsKeys.LocalizationCulture, "bg-BG");
            }
            else
            {
                wnd._settingsService.SetSetting(SettingsKeys.LocalizationCulture, "en-US");
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
            
            _settingsService.SetSetting(SettingsKeys.LocalizationCulture, "bg-BG");
        }

        private ISettingsService _settingsService;

        private SettingsUIExtensionContainer _extContainer;

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
