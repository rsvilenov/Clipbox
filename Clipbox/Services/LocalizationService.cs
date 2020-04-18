using Clipbox.Core.Localization;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Globalization;

namespace Clipbox.Services
{
    public class LocalizationService : ILocalizationService, IDisposable
    {
        private readonly ITranslationSource _translationSource;
        private readonly ISettingsService _settingsService;

        public LocalizationService(ITranslationSource translationSource, ISettingsService settingsService)
        {
            _translationSource = translationSource;
            _settingsService = settingsService;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.SettingKey == SettingsKeys.LicalizationCulture)
            {
                _translationSource.CurrentCulture = new CultureInfo((string)e.SettingValue);
            }
        }

        public void Dispose()
        {
            _settingsService.SettingChanged -= _settingsService_SettingChanged;
        }
    }
}
