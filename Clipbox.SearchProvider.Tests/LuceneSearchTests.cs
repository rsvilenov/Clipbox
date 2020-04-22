using Clipbox.Core.Search;
using Clipbox.Core.Search.IndexUnits;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Search.SearchProviders;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Infrastructure.Search.Tests
{
    [TestFixture]
    public class LuceneSearchTests
    {
        private ISearchProvider _searchProvicer;

        [SetUp]
        public void SetUp()
        {
            _searchProvicer = new LuceneSearch();
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
