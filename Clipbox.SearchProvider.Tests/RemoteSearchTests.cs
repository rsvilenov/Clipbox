using Clipbox.Core.Search;
using Clipbox.Core.Search.IndexUnits;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Search.SearchProviders;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Infrastructure.Search.Tests
{
    [TestFixture]
    public class RemoteSearchTests
    {
        private const string TEST_PIPE_NAME = "testPipe";

        private ISearchProvider _actualSearchProvicer;
        private IEncryptionProvider _encryptionProvider;

        [SetUp]
        public void SetUp()
        {
            _actualSearchProvicer = MockRepository.GenerateMock<ISearchProvider>();

            IndexEntry storedIe = null;

            _actualSearchProvicer.Stub(pp => pp.StoreIndexEntries(Arg<IEnumerable<IndexEntry>>.Is.Anything))
                .WhenCalled(w => storedIe = ((IEnumerable<IndexEntry>)w.Arguments[0]).First());

            _actualSearchProvicer.Stub(pp => pp.FindIndexEntryIds(Arg<IEnumerable<ISearchTerm>>.Is.Anything))
                .WhenCalled(w => w.ReturnValue = storedIe != null ? new List<string> { storedIe.Id } : null)
                .Return(storedIe != null ? new List<string> { storedIe.Id } : null);

            _actualSearchProvicer.Stub(pp => pp.DeleteIndexEntries(Arg<IEnumerable<string>>.Is.Anything))
                .WhenCalled(w => storedIe = null);


            _encryptionProvider = MockRepository.GenerateMock<IEncryptionProvider>();

            byte[] encryptedData = null;

            _encryptionProvider.Stub(pp => pp.Encrypt(Arg<byte[]>.Is.Anything))
                .WhenCalled(w =>
                {
                    encryptedData = (byte[])w.Arguments[0];
                    w.ReturnValue = encryptedData;
                })
                .Return(encryptedData);

            _encryptionProvider.Stub(pp => pp.Decrypt(Arg<byte[]>.Is.Anything))
                .WhenCalled(w => w.ReturnValue = encryptedData)
                .Return(encryptedData);
        }

        [Test]
        public void TestSearchServerMode()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RemoteSearch(TEST_PIPE_NAME, false));
            Assert.Throws<ArgumentNullException>(() =>
               new RemoteSearch(TEST_PIPE_NAME, false, _actualSearchProvicer));
            
            ManualResetEvent mre = new ManualResetEvent(false);
            bool hasError = false;
            Task.Run(() =>
            {
                RemoteSearch serverRs = new RemoteSearch(TEST_PIPE_NAME, false, _actualSearchProvicer, _encryptionProvider);
                serverRs.ErrorOccurred += (s, a) => hasError = true;
                mre.WaitOne();
            });

            mre.Set();
            mre.Dispose();

            Assert.IsFalse(hasError);
        }

        [Test]
        public void TestSearchClientMode()
        {
            string value = Guid.NewGuid().ToString();
            string key = Guid.NewGuid().ToString();
            IndexEntry indexEntry = new IndexEntry(key, new List<IIndexUnit>
            {
                new TextIndexUnit(new List<string> { value })
            });

            TextSearchTerm searchTerm = new TextSearchTerm(new string[] { value });

            ManualResetEvent mre = new ManualResetEvent(false);
            Task.Run(() =>
            {
                RemoteSearch serverRs = new RemoteSearch(TEST_PIPE_NAME, false, _actualSearchProvicer, _encryptionProvider);
                mre.WaitOne();
            });

            try
            {
                ISearchProvider clientRs = new RemoteSearch(TEST_PIPE_NAME, true);
                Assert.DoesNotThrow(() => 
                    clientRs.StoreIndexEntries(new List<IndexEntry> { indexEntry }));

                IEnumerable<string> searchResult = null;

                Assert.DoesNotThrow(() =>
                    searchResult = clientRs.FindIndexEntryIds(new List<ISearchTerm> { searchTerm }));

                Assert.IsNotNull(searchResult);
                Assert.IsNotEmpty(searchResult);
                Assert.AreEqual(searchResult.First(), key);

                Assert.DoesNotThrow(() => 
                    clientRs.DeleteIndexEntries(new string[] { key }));

                searchResult = null;

                Assert.DoesNotThrow(() =>
                    searchResult = clientRs.FindIndexEntryIds(new List<ISearchTerm> { searchTerm }));

                Assert.IsNull(searchResult);
            }
            finally
            {
                mre.Set();
                mre.Dispose();
            }

        }
    }
}
