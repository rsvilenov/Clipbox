using Clipbox.Core.Search;
using Clipbox.Core.Search.IndexUnits;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Search.SearchProviders;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Search.Tests
{
    [TestFixture]
    public class LuceneSearchTests
    {
        private ISearchProvider _searchProvicer;
        private IPersistenceProvider _mockPersistenceProvider;
        private AesEncryptionProvider _shaEncryptionProvider;

        [SetUp]
        public void SetUp()
        {
            _mockPersistenceProvider = MockRepository.GenerateMock<IPersistenceProvider>();

            byte[] generatedRandomKey = null;

            _mockPersistenceProvider.Stub(pp => pp.Write(Arg<string>.Is.Anything, Arg<byte[]>.Is.Anything, Arg<string>.Is.Anything))
                .WhenCalled(w => generatedRandomKey = (byte[])w.Arguments[1]);

            _mockPersistenceProvider.Stub(pp => pp.ReadBytes(Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .WhenCalled(w => w.ReturnValue = generatedRandomKey)
                .Return(generatedRandomKey);

            _shaEncryptionProvider = new AesEncryptionProvider(_mockPersistenceProvider);
            _searchProvicer = new LuceneSearch(_shaEncryptionProvider, _mockPersistenceProvider);
        }

        [Test]
        public void LuceneSearch_StoreSearchDeleteEntry()
        {
            string value = Guid.NewGuid().ToString();
            string key = Guid.NewGuid().ToString();
            IndexEntry ie = new IndexEntry(key, new List<IIndexUnit>
            {
                new TextIndexUnit(new List<string> { value })
            });

            _searchProvicer.StoreIndexEntries(new List<IndexEntry> { ie });


            ISearchTerm textSearchTerm = new TextSearchTerm(new List<string> { value });
            IEnumerable<string> matchingIds = _searchProvicer.FindIndexEntryIds(new List<ISearchTerm> { textSearchTerm });

            Assert.IsTrue(matchingIds.Contains(key));
            
            _searchProvicer.DeleteIndexEntries(new List<string> { key });

            matchingIds = _searchProvicer.FindIndexEntryIds(new List<ISearchTerm> { textSearchTerm });

            Assert.IsFalse(matchingIds.Contains(key));
        }
    }
}
