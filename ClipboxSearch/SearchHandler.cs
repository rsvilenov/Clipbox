using Clipbox.Core;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.Search.SearchProviders;

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
            _searchProvider = new LuceneSearch(_encryptionProvider);
            _remoteSearch = new RemoteSearch(ProductConstants.SEARCH_SERVICE_NAMED_PIPE_PREFIX, false, _searchProvider);
        }

        public void ShutDown()
        {

        }
    }
}
