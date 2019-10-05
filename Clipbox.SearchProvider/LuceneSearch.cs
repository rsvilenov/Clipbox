using Clipbox.Core.Search;
using Clipbox.Core.Search.Enums;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Clipbox.SearchProvider
{
    public class LuceneSearch : ISearchProvider
    {
        private const int LUCENE_SEARCH_MAX_RESULT_COUNT = 10000;

        private readonly string _searchUnitIdFieldName;

        public LuceneSearch()
        {
            var searchUnitObj = new SearchUnit(null, null);
            _searchUnitIdFieldName = nameof(searchUnitObj.Id);
        }

        public void DeleteSearchUnits(IEnumerable<string> searchUnitIds)
        {
            DirectoryInfo dirInfo = GetIndexDirectoryInfo();
            using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            using (var indexDir = FSDirectory.Open(dirInfo))
            {
                using (var indexWriter = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    foreach (string unitId in searchUnitIds)
                    {
                        var searchQuery = new TermQuery(new Term(_searchUnitIdFieldName, unitId));
                        indexWriter.DeleteDocuments(searchQuery);
                    }
                }
            }
        }

        public IEnumerable<string> FindSearchUnitIds(IEnumerable<ISearchTerm> searchTerms)
        {
            List<string> searchUnitIds = new List<string>();

            DirectoryInfo dirInfo = GetIndexDirectoryInfo();
            
            using (var indexDir = FSDirectory.Open(dirInfo))
            using (var indexReader = IndexReader.Open(indexDir, true))
            using (var indexSearcher = new IndexSearcher(indexReader))
            using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            {
                QueryParser queryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30,
                    GetDefaultSearchField(searchTerms),
                    analyzer)
                {
                    AllowLeadingWildcard = true
                };

                var result = indexSearcher.Search(queryParser.Parse(BuildLuceneQuery(searchTerms)),
                    LUCENE_SEARCH_MAX_RESULT_COUNT);

                foreach (var item in result.ScoreDocs)
                {
                    searchUnitIds.Add(indexReader.Document(item.Doc).GetField(_searchUnitIdFieldName).StringValue);
                }
            }

            return searchUnitIds;
        }

        private string GetDefaultSearchField(IEnumerable<ISearchTerm> searchTerms)
        {
            if (searchTerms.Any(st => st.Category == SearchCategory.TextFields))
            {
                return SearchCategory.TextFields.ToString();
            }

            return searchTerms.First().Category.ToString();
        }

        private string BuildLuceneQuery(IEnumerable<ISearchTerm> searchTerms)
        {
            StringBuilder luceneQuery = new StringBuilder();

            int searchTermsCount = searchTerms.Count();
            for (int i = 0; i < searchTermsCount; i++)
            {
                if (i > 0 && i != searchTermsCount - 1)
                {
                    luceneQuery.Append(" AND ");
                }

                ISearchTerm searchTerm = searchTerms.ElementAt(i);
                string category = searchTerm.Category.ToString();
                IEnumerable<string> searchStringParts = GetTermValuesAsStringCollection(searchTerm);
                
                int partsCount = searchStringParts.Count();
                luceneQuery.Append("(");
                for (int j = 0; j < partsCount; j++)
                {
                    if (j > 0 && j != partsCount - 1)
                    {
                        luceneQuery.Append(" OR ");
                    }

                    luceneQuery.AppendFormat("{0}:\"{1}\"", category, searchStringParts.ElementAt(j));
                }
                luceneQuery.Append(")");
            }

            return luceneQuery.ToString();
        }

        public void StoreSearchUnits(IEnumerable<SearchUnit> searchUnits)
        {
            DirectoryInfo dirInfo = GetIndexDirectoryInfo();

            using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            using (var indexDir = FSDirectory.Open(dirInfo))
            {
                using (var indexWriter = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    foreach (SearchUnit unit in searchUnits)
                    {
                        // remove older index entry
                        var searchQuery = new TermQuery(new Term(nameof(unit.Id), unit.Id));
                        indexWriter.DeleteDocuments(searchQuery);

                        // create a Lucene document
                        Document document = new Document();

                        document.Add(new Field(nameof(unit.Id), unit.Id, Field.Store.YES, Field.Index.NOT_ANALYZED));

                        foreach (ISearchTerm term in unit.SearchTerms)
                        {
                            string stringToStore = string.Join(" ; ", GetTermValuesAsStringCollection(term));
                            document.Add(new Field(term.Category.ToString(), stringToStore, Field.Store.YES, Field.Index.ANALYZED));
                        }

                        indexWriter.AddDocument(document);
                    }
                }
            }
        }

        private static DirectoryInfo GetIndexDirectoryInfo()
        {
            string indexDirectoryPath = Path.Combine(AppDataUtils.GetAppDataDir(false), "searchIdx");
            if (!System.IO.Directory.Exists(indexDirectoryPath))
            {
                System.IO.Directory.CreateDirectory(indexDirectoryPath);
            }

            var dirInfo = new System.IO.DirectoryInfo(indexDirectoryPath);
            return dirInfo;
        }

        private IEnumerable<string> GetTermValuesAsStringCollection(ISearchTerm term)
        {
            if (term is ClipboardContentType)
            {
               return ((ClipboardContentType)term).SearchValues.Select(t => t.ToString());
            }
            else if (term is SourceApplicationName)
            {
                return ((SourceApplicationName)term).SearchValues;
            }
            else if (term is SourceMachineName)
            {
                return ((SourceMachineName)term).SearchValues;
            }
            else if (term is SourceWindowName)
            {
                return ((SourceWindowName)term).SearchValues;
            }
            else if (term is TextFields)
            {
                return ((TextFields)term).SearchValues;
            }
            else if (term is Tag)
            {
                return ((Tag)term).SearchValues;
            }
            else
            {
                throw new ArgumentException("The term's type is not recognized.");
            }
        }
    }
}
