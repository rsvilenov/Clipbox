using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Search;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.SearchProvider.Tests
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
            SearchUnit su = new SearchUnit(key, new List<ISearchTerm>
            {
                new TextSearchTerm(new List<string> { value })
            });

            _searchProvicer.StoreSearchUnits(new List<SearchUnit> { su });


            ISearchTerm textSearchTerm = new TextSearchTerm(new List<string> { value });
            IEnumerable<string> matchingIds = _searchProvicer.FindSearchUnitIds(new List<ISearchTerm> { textSearchTerm });

            Assert.IsTrue(matchingIds.Contains(key));
            
            _searchProvicer.DeleteSearchUnits(new List<string> { key });

            matchingIds = _searchProvicer.FindSearchUnitIds(new List<ISearchTerm> { textSearchTerm });

            Assert.IsFalse(matchingIds.Contains(key));
        }
    }
}
