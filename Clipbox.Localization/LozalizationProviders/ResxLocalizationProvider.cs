using Clipbox.Infrastructure.UI.Localization.EventArgsClasses;
using System;
using System.Globalization;
using System.Resources;

namespace Clipbox.Infrastructure.UI.Localization.LozalizationProviders
{
    public class ResxLocalizationProvider : ILocalizationProvider
    {
        private readonly ResourceManager _resourceManager;

        public ResxLocalizationProvider(ResourceManager resManager)
        {
            _resourceManager = resManager;
        }

        public string GetTranslation(string key)
        {
            return _resourceManager.GetString(key);
        }

        private CultureInfo _currentCulture;

        public event EventHandler<CultureChangedEventArgs> LocalizationCultureChanged;

        public CultureInfo CurrentCulture
        {
            get { return _currentCulture; }
            set
            {
                if (_currentCulture != value)
                {
                    CultureInfo oldCulture = _currentCulture;
                    _currentCulture = value;
                    LocalizationCultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, _currentCulture));
                }
            }
        }
    }
}
