using Clipbox.Core;
using Clipbox.Core.Search;
using Clipbox.Core.Search.Enums;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.FilesystemAccess;
using Clipbox.Infrastructure.Search.SearchProviders.Encryption;
using Contrib.Regex;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Clipbox.Infrastructure.Search.SearchProviders
{
    public class LuceneSearch : ISearchProvider
    {
        private const int LUCENE_SEARCH_MAX_RESULT_COUNT = 10000;
        private const int LUCENE_MAX_CLAUSES_COUNT = 50000;
        private const string LUCENE_READY_FILENAME = "lucene.ready";

        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IPersistenceProvider _persistenceProvider;

        public LuceneSearch(IEncryptionProvider encryptionProvider, 
            IPersistenceProvider persistenceProvider)
        {
            // Despite the fact that am telling Lucene not to store 
            // the original items in the index, there are techniques 
            // by which the value can be partially restored, and that's
            // why I want to encrypt the whole index.
            _encryptionProvider = encryptionProvider;

            // I am using this only to write the error logs.
            // Maybe in the futire I may modify the IndexWriter/Reader
            // classes to make them use the provider.
            _persistenceProvider = persistenceProvider;

            SetUpLuceneIfFirstRun();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ProcessIfLuceneException(ex, throwOnTooManyErrors: false);
            }
        }

        private bool ProcessIfLuceneException(Exception ex, bool throwOnTooManyErrors = true)
        {
            if (ex.StackTrace.Contains("Lucene.Net"))
            {
                IEnumerable<string> errorLogs = _persistenceProvider.ListKeys(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY);

                int errorLogsCount = errorLogs.Count();

                _persistenceProvider.Write(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY,
                    errorLogsCount.ToString(),
                    ex.ToString());

                _persistenceProvider.Append(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY,
                    ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE,
                    ex.ToString());

                if (errorLogsCount > 3
                    && throwOnTooManyErrors)
                {
                    throw new InvalidOperationException("Too many subsequent errors from LuceneSearch.");
                }
                
                return true;
            }

            return false;
        }

        public void DeleteIndexEntries(IEnumerable<string> indexEntryIds)
        {
            if (!indexEntryIds.Any())
            {
                throw new ArgumentException($"{nameof(indexEntryIds)} is empty");
            }

            DirectoryInfo dirInfo = GetIndexDirectoryInfo();

            try
            {
                using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
                using (var indexDir = new EncryptedFSDirectory(dirInfo, _encryptionProvider, null))
                {
                    using (var indexWriter = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        foreach (string unitId in indexEntryIds)
                        {
                            var searchQuery = new TermQuery(new Term(nameof(IndexEntry.Id), unitId));
                            indexWriter.DeleteDocuments(searchQuery);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ProcessIfLuceneException(ex))
                {
                    throw;
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
            if (!dirInfo.GetFiles().Any())
            {
                // if the folder is empty, then there is no indexed content yet.
                return searchUnitIds;
            }

            try
            {
                using (var indexDir = new EncryptedFSDirectory(dirInfo, _encryptionProvider, null))
                using (var indexReader = IndexReader.Open(indexDir, true))
                using (var indexSearcher = new IndexSearcher(indexReader))
                {
                    IEnumerable<ScoreDoc> scoreDocs = RunLiceneQuery(indexSearcher,
                        ConstructLuceneQuery(searchTerms),
                        LUCENE_SEARCH_MAX_RESULT_COUNT);

                    foreach (var scoreDoc in scoreDocs)
                    {
                        searchUnitIds.Add(indexReader.Document(scoreDoc.Doc).GetField(nameof(IndexEntry.Id)).StringValue);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ProcessIfLuceneException(ex))
                {
                    throw;
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
                    else if (searchTerm is MinCreationDateSearchTerm minCreationDateSt)
                    {
                        var lowerRange = new TermRangeQuery(category, minCreationDateSt.Values.Single().ToString(), null, true, true);
                        subQuery.Add(lowerRange, Occur.MUST);
                    }
                    else if (searchTerm is MaxCreationDateSearchTerm maxCreationDateSt)
                    {
                        var upperRange = new TermRangeQuery(category, null, maxCreationDateSt.Values.Single().ToString(), true, true);
                        subQuery.Add(upperRange, Occur.MUST);
                    }
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

            try
            {
                using (var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
                using (var indexDir = new EncryptedFSDirectory(dirInfo, _encryptionProvider, null))
                {
                    using (var indexWriter = new IndexWriter(indexDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        // When Lucene uses parralel merge, it creates threads and
                        // upon merge error, it throws exceptions from these threads.
                        // This means that when this happens, our app domain is gone...
                        // So, we will just swith to SerialMerge instead.
                        indexWriter.SetMergeScheduler(new SerialMergeScheduler());

                        foreach (IndexEntry entry in indexEntries)
                        {
                            // remove older index entry
                            var searchQuery = new TermQuery(new Term(nameof(IndexEntry.Id), entry.Id));
                            indexWriter.DeleteDocuments(searchQuery);

                            // create a Lucene document
                            Document document = new Document();

                            document.Add(new Field(nameof(IndexEntry.Id), entry.Id, Field.Store.YES, Field.Index.NOT_ANALYZED));

                            foreach (IIndexUnit unit in entry.IndexUnits)
                            {
                                string stringToStore = string.Join(" ; ", unit.StringValues);
                                document.Add(new Field(unit.Category.ToString(), stringToStore, Field.Store.NO, Field.Index.ANALYZED));
                            }

                            indexWriter.AddDocument(document, analyzer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ProcessIfLuceneException(ex))
                {
                    throw;
                }
            }
        }

        private void SetUpLuceneIfFirstRun()
        {
            string appDataPath = AppDataUtils.GetAppDataDir(false);
            string luceneReadyFilePath = System.IO.Path.Combine(appDataPath, LUCENE_READY_FILENAME);

            if (!System.IO.File.Exists(luceneReadyFilePath))
            {
                string indexDirectoryPath = System.IO.Path.Combine(appDataPath, "searchIdx");
                if (!System.IO.Directory.Exists(indexDirectoryPath))
                {
                    System.IO.Directory.CreateDirectory(indexDirectoryPath);
                    System.IO.File.WriteAllText(luceneReadyFilePath, "ok");
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

        public void ClearIndexEntries()
        {
            GetIndexDirectoryInfo().Delete(recursive: true);
        }

        public IndexHealthInfo GetIndexHealth()
        {
            string indexDirectoryPath = Path.Combine(AppDataUtils.GetAppDataDir(false), "searchIdx");
            if (!Directory.Exists(indexDirectoryPath))
            {
                return new IndexHealthInfo(false);
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE))
            {
                string aggregateErrorText = _persistenceProvider.ReadText(ProductConstants.SEARCH_INDEX_ERROR_LOG_CATEGORY, 
                    ProductConstants.SEARCH_INDEX_ERROR_LOG_AGGREGATE);

                return new IndexHealthInfo(false, aggregateErrorText);
            }
            else
            {
                return new IndexHealthInfo(true);
            }
        }
    }
}
