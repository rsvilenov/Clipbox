using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class LocalizationService : ILocalizationService, IDisposable
    {
        private readonly ILocalizationProvider _translationProvider;
        private readonly ISettingsService _settingsService;

        public LocalizationService(ILocalizationProvider translationProvider, ISettingsService settingsService)
        {
            _translationProvider = translationProvider;
            _settingsService = settingsService;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.SettingKeyName == SettingsKeys.LocalizationCulture.Name)
            {
                var newCulture = new CultureInfo((string)e.SettingValue);
                _translationProvider.CurrentCulture = newCulture;
                UpdateCurrentUICulture(newCulture);
            }
        }

        private void UpdateCurrentUICulture(CultureInfo culture)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            });
        }

        public void Dispose()
        {
            _settingsService.SettingChanged -= _settingsService_SettingChanged;
        }
    }
}
