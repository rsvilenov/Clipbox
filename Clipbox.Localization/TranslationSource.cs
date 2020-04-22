using System;
using System.ComponentModel;

namespace Clipbox.Infrastructure.UI.Localization
{
    public class TranslationSource : INotifyPropertyChanged
    {
        private static TranslationSource _instance;
        public static TranslationSource Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"Please call {nameof(InitTranslationSource)} first.");
                }

                return _instance;
            }
        }

        public static void InitTranslationSource(ILocalizationProvider localizationProvider)
        {
            _instance = new TranslationSource(localizationProvider);
        }

        private readonly ILocalizationProvider _localizationProvider;

        private TranslationSource(ILocalizationProvider localizationProvider)
        {
            _localizationProvider = localizationProvider;
            _localizationProvider.LocalizationCultureChanged += LocalizatioProvider_LocalizationCultureChanged;
        }

        private void LocalizatioProvider_LocalizationCultureChanged(object sender, EventArgsClasses.CultureChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
        
        public string this[string key]
        {
            get { return _localizationProvider.GetTranslation(key); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
