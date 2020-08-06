using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;

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

            string cultureCode = _settingsService.GetSetting<string>(SettingsKeys.LocalizationCulture);
            SetCulture(cultureCode);
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.LocalizationCulture)
            {
                SetCulture((string)e.Value);
            }
        }

        private void SetCulture(string cultureCode)
        {
            var newCulture = new CultureInfo(cultureCode);
            UpdateCurrentUICulture(newCulture);
            _translationProvider.CurrentCulture = newCulture;
        }

        private void UpdateCurrentUICulture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            });

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public void Dispose()
        {
            _settingsService.SettingChanged -= _settingsService_SettingChanged;
        }
    }
}
