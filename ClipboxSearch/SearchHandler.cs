using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Core.Enums;
using Clipbox.Infrastructure.Diagnostics;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.Search.SearchProviders;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Win32;
using System;
using System.Windows;
using System.Windows.Threading;

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
            // exceptions from this line will go to the stderr
            // and will be captured by the guard.
            _persistenceProvider = new AppDataPersistenceProvider();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ClearLastExceptionInfo();

            _encryptionProvider = new AesEncryptionProvider(_persistenceProvider);
            _searchProvider = new LuceneSearch(_encryptionProvider, _persistenceProvider);

            int sessionId = SessionUtils.GetCurrentSessionId();
            _remoteSearch = new RemoteSearch($"{ProductConstants.SEARCH_SERVICE_NAMED_PIPE_PREFIX}.{sessionId}", false, _searchProvider);

            _remoteSearch.QuitRequested += _remoteSearch_QuitRequested;
        }

        private void ClearLastExceptionInfo()
        {
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_PROCESS_FAILURE_XML_PERSISTENCE_KEY))
            {
                _persistenceProvider.Delete(ProductConstants.SEARCH_PROCESS_FAILURE_XML_PERSISTENCE_KEY);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;
            try
            {
                DiagnosticsReport dr = DiagnosticsReportGenerator
                    .Generate(exception, includeExtensionInfo: false);
                string reportXml = XmlSerialization.Serialize(dr);
                
                _persistenceProvider.Write(ProductConstants.SEARCH_PROCESS_FAILURE_XML_PERSISTENCE_KEY, reportXml);
            }
            catch (Exception ex)
            {
                // this does not create a recursion with CurrentDomain_UnhandledException 
                // so it is safe to do.
                throw new AggregateException(exception, ex);
            }

            Environment.Exit((int)ProcessExitCodes.Error);
        }

        private void _remoteSearch_QuitRequested(object sender, System.EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => Application.Current.Shutdown((int)ProcessExitCodes.Ok)));
        }

        public void ShutDown()
        {

        }
    }
}
