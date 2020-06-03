using Clipbox.Core.Search;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Search.SearchProviders.EventArgsClasses;
using Clipbox.Infrastructure.Serialization;
using System;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Search.SearchProviders
{
    public class RemoteSearch : ISearchProvider
    {
        private readonly bool _isCliendMode;
        private readonly NamedPipesServer _pipeServer;
        private readonly NamedPipesClient _pipeClient;
        private readonly ISearchProvider _actualSerchProvider;

        public event EventHandler<RemoteSearchErrorEventArgs> ErrorOccurred;

        public RemoteSearch(string pipeName, 
            bool isClientMode,
            ISearchProvider actualSerchProvider,
            IEncryptionProvider encryptionProvider = null)
        {
            _isCliendMode = isClientMode;
            _actualSerchProvider = actualSerchProvider;

            if (_isCliendMode)
            {
                _pipeClient = new NamedPipesClient(pipeName);
            }
            else
            {
                if (encryptionProvider == null)
                {
                    throw new ArgumentNullException($"Passing a valid provider for argument '{nameof(encryptionProvider)}' is obligatory for server mode.");
                }

                _pipeServer = new NamedPipesServer(pipeName);

                _actualSerchProvider = new LuceneSearch(encryptionProvider);
                _pipeServer.GotMessage += _pipeServer_GotMessage;
                _pipeServer.ErrorOccurred += _pipeServer_ErrorOccurred;
            }
        }

        private void _pipeServer_ErrorOccurred(object sender, NamedPipeErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, 
                new RemoteSearchErrorEventArgs("Pipe server error.", e.Exception));
        }

        private void _pipeServer_GotMessage(object sender, NamedPipesResponseEventArgs e)
        {
            byte[] answer = Convert.FromBase64String(e.ReceivedMessage);
            SearchPipeMessage msg = BinaryFormatterSerialization.DeserializeObject<SearchPipeMessage>(answer);
            ISearchProvider searchProvider = this;

            switch (msg.MessageType)
            {
                case SearchPipeMessageType.Store:
                    searchProvider.StoreIndexEntries(msg.IndexEntries);
                    break;
                case SearchPipeMessageType.Search:
                    SearchPipeMessage responseMsg = new SearchPipeMessage
                    {
                        MessageType = SearchPipeMessageType.Result
                    };

                    responseMsg.IndexEntryIds = searchProvider.FindIndexEntryIds(msg.SearchTerms);
                    byte[] responseBytes = BinaryFormatterSerialization.SerializeObject(responseMsg);
                    e.Response = Convert.ToBase64String(responseBytes);
                    break;
                case SearchPipeMessageType.Delete:
                    searchProvider.DeleteIndexEntries(msg.IndexEntryIds);
                    break;
                default:
                    throw new InvalidOperationException($"Message type \"{msg.MessageType}\" is not supported.");
            }
        }

        void ISearchProvider.DeleteIndexEntries(IEnumerable<string> indexEntryIds)
        {
            if (_isCliendMode)
            {
                SearchPipeMessage msg = new SearchPipeMessage
                {
                    IndexEntryIds = indexEntryIds,
                    MessageType = SearchPipeMessageType.Delete
                };

                byte[] serialized = BinaryFormatterSerialization.SerializeObject(msg);

                if (!_pipeClient.SendMessage(Convert.ToBase64String(serialized)))
                {
                    throw new IpcException("Errpr sending message to pipe server");
                }
            }
            else
            {
                _actualSerchProvider.DeleteIndexEntries(indexEntryIds);
            }
        }

        IEnumerable<string> ISearchProvider.FindIndexEntryIds(IEnumerable<ISearchTerm> searchTerms)
        {
            if (_isCliendMode)
            {
                SearchPipeMessage msg = new SearchPipeMessage
                {
                    SearchTerms = searchTerms,
                    MessageType = SearchPipeMessageType.Search
                };

                byte[] serialized = BinaryFormatterSerialization.SerializeObject(msg);

                if (!_pipeClient.SendMessage(Convert.ToBase64String(serialized), out string response))
                {
                    throw new IpcException("Errpr sending message to pipe server");
                }

                byte[] responseBytes = Convert.FromBase64String(response);
                SearchPipeMessage deserializedResponseMsg = BinaryFormatterSerialization.DeserializeObject<SearchPipeMessage>(responseBytes);
                return deserializedResponseMsg.IndexEntryIds;
            }
            else
            {
                return _actualSerchProvider.FindIndexEntryIds(searchTerms);
            }
        }

        void ISearchProvider.StoreIndexEntries(IEnumerable<IndexEntry> indexEntries)
        {
            if (_isCliendMode)
            {
                SearchPipeMessage msg = new SearchPipeMessage
                {
                    IndexEntries = indexEntries,
                    MessageType = SearchPipeMessageType.Store
                };

                byte[] serialized = BinaryFormatterSerialization.SerializeObject(msg);

                if (!_pipeClient.SendMessage(Convert.ToBase64String(serialized)))
                {
                    throw new IpcException("Errpr sending message to pipe server");
                }
            }
            else
            {
                _actualSerchProvider.StoreIndexEntries(indexEntries);
            }
        }

        [Serializable]
        private class SearchPipeMessage
        {
            public SearchPipeMessageType MessageType { get; set; }

            public IEnumerable<IndexEntry> IndexEntries { get; set; }

            public IEnumerable<ISearchTerm> SearchTerms { get; set; }

            public IEnumerable<string> IndexEntryIds { get; set; }
        }

        private enum SearchPipeMessageType
        {
            Store,
            Search,
            Delete,
            Result
        }


    }
}
