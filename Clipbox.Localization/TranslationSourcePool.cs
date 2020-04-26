using Clipbox.Infrastructure.UI.Localization.Enums;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Clipbox.Infrastructure.UI.Localization
{
    public class TranslationSource : INotifyPropertyChanged
    {
        ILocalizationProvider _locProvider;
        private readonly Assembly _asm;

        public TranslationSource(ILocalizationProvider locProvider, Assembly asm)
        {
            _locProvider = locProvider;
            _asm = asm;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string GetTranslation(string key)
        {
            if (_locProvider is IAssemblyAwareLocalizationProvider asmLevelLocProvider)
            {
                return asmLevelLocProvider.GetTranslation(key,
                    _asm == null ? ResourceLookupMode.Everywhere : ResourceLookupMode.CurrentAssembly,
                    _asm);
            }
            else
            {
                return _locProvider.GetTranslation(key);
            }
        }

        public string this[string key]
        {
            get { return GetTranslation(key); }
        }

        internal void NotifyCultureChange()
        {
            // refresh the binding
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }


    public class TranslationSourcePool
    {
        private static TranslationSourcePool _instance;
        /// <summary>
        /// The single instance of the singleton class
        /// </summary>
        public static TranslationSourcePool Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"Please call {nameof(InitTranslationSourcePool)} first.");
                }

                return _instance;
            }
        }

        /// <summary>
        /// The injected localization provider.
        /// </summary>
        private ILocalizationProvider _localizationProvider;

        /// <summary>
        /// A dictionary to store the TranslationSource instances.
        /// </summary>
        private ConcurrentDictionary<string, TranslationSource> _sources;

        /// <summary>
        /// Sets the ILocalizationProvider and initializes the singleton.
        /// </summary>
        /// <param name="localizationProvider"></param>
        public static void InitTranslationSourcePool(ILocalizationProvider localizationProvider)
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("The singleton is already initialized.");
            }

            _instance = new TranslationSourcePool(localizationProvider);
        }
        
        private TranslationSourcePool(ILocalizationProvider localizationProvider)
        {
            _localizationProvider = localizationProvider;

            _sources = new ConcurrentDictionary<string, TranslationSource>();

            _localizationProvider.LocalizationCultureChanged += _localizationProvider_LocalizationCultureChanged;
        }

        private void _localizationProvider_LocalizationCultureChanged(object sender, EventArgsClasses.CultureChangedEventArgs e)
        {
            _sources
                .Select(s => s.Value) // this should be thread-safe
                .ToList() // make a copy of the collection so that changes in it won't affect us
                .ForEach((source) => source.NotifyCultureChange());
        }
        
        /// <summary>
        /// Gets a translation source
        /// </summary>
        /// <param name="asm">The assembly for which to get the translation source</param>
        /// <returns></returns>
        public TranslationSource GetSource(Assembly asm = null)
        {
            string asmName =asm == null ? "(default)" : asm.FullName;

            if (_sources.TryGetValue(asmName, out TranslationSource src))
            {
                return src;
            }
            else
            {
                var newSrc = new TranslationSource(_localizationProvider, asm);
                _sources.AddOrUpdate(asmName, newSrc, (s, oldSrc) => newSrc);
                return newSrc;
            }
        }

    }
}
