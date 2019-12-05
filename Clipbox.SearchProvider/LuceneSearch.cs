using Clipbox.Core.Search;
using Clipbox.Core.Search.Enums;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Search;
using Contrib.Regex;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Clipbox.SearchProvider
{
    public class LuceneSearch : ISearchProvider
    {
        private const int LUCENE_SEARCH_MAX_RESULT_COUNT = 10000;
        private const int LUCENE_MAX_CLAUSES_COUNT = 50000;

        private readonly string _indexEntryIdFieldName;

        public LuceneSearch()
        {
            var indexEntryObj = new IndexEntry(null, Enumerable.Empty<IIndexUnit>());
            _indexEntryIdFieldName = nameof(indexEntryObj.Id);
        }

        public void DeleteIndexEntries(IEnumerable<string> indexEntryIds)
        {
            if (!indexEntryIds.Any())
            {
                throw new ArgumentException($"{nameof(indexEntryIds)} is empty");
            }

            DirectoryInfo dirInfo = GetIndexDirectoryInfo();
            using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            using (var indexDir = FSDirectory.Open(dirInfo))
            {
                using (var indexWriter = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    foreach (string unitId in indexEntryIds)
                    {
                        var searchQuery = new TermQuery(new Term(_indexEntryIdFieldName, unitId));
                        indexWriter.DeleteDocuments(searchQuery);
                    }
                }
            }
        }

        public IEnumerable<string> FindIndexEntryIds(IEnumerable<ISearchTerm> searchTerms)
        {
            if (!searchTerms.Any())
            {
                throw new ArgumentException($"{nameof(searchTerms)} is empty");
            }

            List<string> searchUnitIds = new List<string>();

            DirectoryInfo dirInfo = GetIndexDirectoryInfo();

            using (var indexDir = FSDirectory.Open(dirInfo))
            using (var indexReader = IndexReader.Open(indexDir, true))
            using (var indexSearcher = new IndexSearcher(indexReader))
            {
                IEnumerable<ScoreDoc> scoreDocs = RunLiceneQuery(indexSearcher, 
                    ConstructLuceneQuery(searchTerms),
                    LUCENE_SEARCH_MAX_RESULT_COUNT);

                foreach (var scoreDoc in scoreDocs)
                {
                    searchUnitIds.Add(indexReader.Document(scoreDoc.Doc).GetField(_indexEntryIdFieldName).StringValue);
                }
            }

            return searchUnitIds;
        }
        
        private string GetDefaultSearchField(IEnumerable<ISearchTerm> searchTerms)
        {
            if (searchTerms.Any(st => st.Category == SearchCategory.Text))
            {
                return SearchCategory.Text.ToString();
            }

            return searchTerms.First().Category.ToString();
        }

        private SpanNearQuery ConstructSpanNearQuery(string fieldName, string phrase, bool appendTrailingWildcard)
        {
            List<SpanQuery> spanQueries = new List<SpanQuery>();
            string[] words = phrase.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                SpanQuery sq = null;

                if (i == words.Length - 1)
                {
                    word = Regex.Escape(word);

                    // if the asterisk, passed with the search string by the user has been regex-escaped, 
                    // unescape it and make it regex-usable (.*).
                    word = word.Replace("\\*", ".*");
                    
                    if (!word.EndsWith(".*") && appendTrailingWildcard)
                    {
                        word = Regex.Escape(word);
                        word = $"{word}.*";
                    }

                    sq = new SpanRegexQuery(new Term(fieldName, word));
                }
                else
                {
                    sq = new SpanTermQuery(new Term(fieldName, word));
                }

                spanQueries.Add(sq);
            }

            return new SpanNearQuery(spanQueries.ToArray(), 
                5 /* the slop is not so important as the user will not expect to skip words when searching */,
                true);
        }


        private Query ConstructLuceneQuery(IEnumerable<ISearchTerm> searchTerms)
        {
            // we assume that MinimumNumberShouldMatch of the BooleanQuery will remain set to 0,
            // as is the default value of that property.
            // If no mandatory terms are included in the boolean query,
            // Lucene will treat our Occur.Should as Occur.Must.
            if (searchTerms.Count(st => st.Occurrence == TermOccurrence.OBLIGATORY || st.Occurrence == TermOccurrence.NON_PRESENT) == 0)
            {
                throw new InvalidOperationException($"The list of search terms must include at least one clause with occurrance set to {TermOccurrence.OBLIGATORY} or {TermOccurrence.NON_PRESENT}");
            }

            BooleanQuery bq = new BooleanQuery();
            
            StringBuilder luceneQuery = new StringBuilder();

            int searchTermsCount = searchTerms.Count();
            for (int i = 0; i < searchTermsCount; i++)
            {
                BooleanQuery subQuery = new BooleanQuery();

                ISearchTerm searchTerm = searchTerms.ElementAt(i);
                string category = searchTerm.Category.ToString();
                IEnumerable<string> searchStringParts = searchTerm.StringValues;
                
                int partsCount = searchStringParts.Count();
                for (int j = 0; j < partsCount; j++)
                {
                    string part = searchStringParts.ElementAt(j).ToLower(); // the StandartAnalyzer makes everthing lowercase during the indexing
                    if (searchTerm is ClipboardContentTypeSearchTerm)
                    {
                        subQuery.Add(new TermQuery(new Term(category, part)), Occur.SHOULD /* OR */);
                    }
                    //else if (searchTerm is TextSearchTerm)
                    //{
                    //    subQuery.Add(ConstructSpanNearQuery(category, part, appendTrailingWildcard: true), Occur.SHOULD);
                    //}
                    else
                    {
                        subQuery.Add(ConstructSpanNearQuery(category, part, appendTrailingWildcard: false), Occur.SHOULD);
                    }
                }

                Occur luceneTermRelation;
                switch (searchTerm.Occurrence)
                {
                    case TermOccurrence.OBLIGATORY:
                        luceneTermRelation = Occur.MUST;
                        break;
                    case TermOccurrence.NON_OBLIGATORY:
                        luceneTermRelation = Occur.SHOULD;
                        break;
                    case TermOccurrence.NON_PRESENT:
                        luceneTermRelation = Occur.MUST_NOT;
                        break;
                    default:
                        throw new NotSupportedException($"{nameof(TermOccurrence)}.{searchTerm.Occurrence} is not supported by thus class.");
                }
                bq.Add(subQuery, luceneTermRelation);
            }

            return bq;
        }

        public void StoreIndexEntries(IEnumerable<IndexEntry> indexEntries)
        {
            if (!indexEntries.Any())
            {
                throw new ArgumentException($"{nameof(indexEntries)} is empty");
            }

            DirectoryInfo dirInfo = GetIndexDirectoryInfo();

            using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            using (var indexDir = FSDirectory.Open(dirInfo))
            {
                using (var indexWriter = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    foreach (IndexEntry entry in indexEntries)
                    {
                        // remove older index entry
                        var searchQuery = new TermQuery(new Term(_indexEntryIdFieldName, entry.Id));
                        indexWriter.DeleteDocuments(searchQuery);

                        // create a Lucene document
                        Document document = new Document();

                        document.Add(new Field(_indexEntryIdFieldName, entry.Id, Field.Store.YES, Field.Index.NOT_ANALYZED));

                        foreach (IIndexUnit unit in entry.IndexUnits)
                        {
                            string stringToStore = string.Join(" ; ", unit.StringValues);
                            document.Add(new Field(unit.Category.ToString(), stringToStore, Field.Store.NO, Field.Index.ANALYZED));
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

        /// <summary>
        /// If there are a lot of indexed items and we are using wildcard/regex search terms,
        /// then Lucene materializes the terms by creating a separate term for each possible
        /// match, thus the possibility of exceding the default max search term limit of 1024.
        /// So we need to catch the exception and increase the limit as long as we reach our
        /// own search limit, defined in <see cref="LuceneSearch.LUCENE_MAX_CLAUSES_COUNT"/>.
        /// This is a workaround, but there does not seem to exist a better solution for the moment.
        /// </summary>
        /// <param name="searcher"></param>
        /// <param name="query"></param>
        /// <param name="maxResultCount"></param>
        /// <returns></returns>
        private IEnumerable<ScoreDoc> RunLiceneQuery(IndexSearcher searcher, Query query, int maxResultCount)
        {
            IEnumerable<ScoreDoc> scoreDocs = null;

            while (BooleanQuery.MaxClauseCount < LUCENE_MAX_CLAUSES_COUNT)
            {
                try
                {
                    TopDocs td = searcher.Search(query, maxResultCount);

                    if (td != null && td.ScoreDocs != null)
                    {
                        scoreDocs = td.ScoreDocs;
                    }

                    break;
                }
                catch (BooleanQuery.TooManyClauses)
                {
                    BooleanQuery.MaxClauseCount *= 2;
                }
            }

            return scoreDocs ?? Enumerable.Empty<ScoreDoc>();
        }
    }
}
