using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI.Localization
{
    public class TranslationSource : INotifyPropertyChanged
    {
        private static readonly Lazy<TranslationSource> _lazyInstance =
            new Lazy<TranslationSource>(() => new TranslationSource());
        
        public static TranslationSource Instance
        {
            get { return _lazyInstance.Value; }
        }
        private readonly ResourceManager _resourceManager;
        private CultureInfo _currentCulture = null;

        private TranslationSource()
        {
            _resourceManager = Resources.Strings.ResourceManager;
        }
        
        public string this[string key]
        {
            get { return _resourceManager.GetString(key, _currentCulture); }
        }

        public CultureInfo CurrentCulture
        {
            get { return _currentCulture; }
            set
            {
                if (_currentCulture != value)
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                    if (dispatcher == null)
                    {
                        throw new InvalidOperationException("Please call this setter from the UI thread.");
                    }

                    _currentCulture = value;
                    
                    Thread.CurrentThread.CurrentCulture = _currentCulture;
                    Thread.CurrentThread.CurrentUICulture = _currentCulture;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
