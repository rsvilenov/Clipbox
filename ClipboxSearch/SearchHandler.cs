using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.Search.SearchProviders;
using Clipbox.Win32;
using System.Windows;

namespace ClipboxSearch
{
    public class SearchHandler
    {
        private ISearchProvider _searchProvider;
        private IEncryptionProvider _encryptionProvider;
        private IPersistenceProvider _persistenceProvider;
        private RemoteSearch _remoteSearch;

        public void StartUp()
        {
            _persistenceProvider = new AppDataPersistenceProvider();
            _encryptionProvider = new AesEncryptionProvider(_persistenceProvider);
            _searchProvider = new LuceneSearch(_encryptionProvider, _persistenceProvider);

            int sessionId = SessionUtils.GetCurrentSessionId();
            _remoteSearch = new RemoteSearch($"{ProductConstants.SEARCH_SERVICE_NAMED_PIPE_PREFIX}.{sessionId}", false, _searchProvider);

            _remoteSearch.QuitRequested += _remoteSearch_QuitRequested;
        }

        private void _remoteSearch_QuitRequested(object sender, System.EventArgs e)
        {
            Application.Current.Shutdown((int)ProcessExitCodes.Ok);
        }

        public void ShutDown()
        {

        }
    }
}
