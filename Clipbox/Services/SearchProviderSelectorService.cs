using Clipbox.Core;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.Search.SearchProviders;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;

namespace Clipbox.Services
{
    public class SearchProviderSelectorService : ISearchProviderSelectorService
    {
        private readonly ISettingsService _settingsService;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IPersistenceProvider _persistenceProvider;
        private readonly int _sessionId;

        public SearchProviderSelectorService(ISettingsService settingsService,
            IPersistenceProvider persistenceProvider,
            IEncryptionProvider encryptionProvider,
            int sessionId)
        {
            _settingsService = settingsService;
            _encryptionProvider = encryptionProvider;
            _persistenceProvider = persistenceProvider;
            _sessionId = sessionId;

            SetOrUnsetPersistentFlagForExternalProcess();
            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.UseSearchProviderInExternalProcess)
            {
                SetOrUnsetPersistentFlagForExternalProcess();
            }
        }

        private void SetOrUnsetPersistentFlagForExternalProcess()
        {
            bool useExternalProcess = _settingsService.GetSetting<bool>(SettingsKeys.UseSearchProviderInExternalProcess);
            if (useExternalProcess)
            {
                _persistenceProvider.Write(ProductConstants.SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY, string.Empty);
            }
            else
            {
                _persistenceProvider.Delete(ProductConstants.SEARCH_USE_EXTERNAL_PROCESS_FLAG_KEY);
            }
        }

        public ISearchProvider GetSearchProvider()
        {
            bool useExternalProcess = _settingsService.GetSetting<bool>(SettingsKeys.UseSearchProviderInExternalProcess);
            
            ISearchProvider searchProvider = useExternalProcess
                ? (ISearchProvider)new RemoteSearch($"{ProductConstants.SEARCH_SERVICE_NAMED_PIPE_PREFIX}.{_sessionId}", true)
                : new LuceneSearch(_encryptionProvider, _persistenceProvider);

            return searchProvider;
        }
    }
}
