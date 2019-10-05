using Clipbox.Core.Search;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Services
{
    public class SearchService : ISearchService, IDisposable
    {
        private readonly ISearchProvider _searchProvider;
        private readonly SlidingTimer _clipboardItemsChangeSlidingTimer;

        private readonly List<string> _removedSearchUnitIds;
        private readonly List<SearchUnit> _addedSearchUnits;
        
        private readonly object _listLockHandle = new object();

        private const int STORE_INTERVAL_MS = 3000;
        
        private ManualResetEvent _lenghtySearchStoreOperationsMre;

        public SearchService(ISearchProvider searchProvider)
        {
            _searchProvider = searchProvider;

            _removedSearchUnitIds = new List<string>();
            _addedSearchUnits = new List<SearchUnit>();

            _lenghtySearchStoreOperationsMre = new ManualResetEvent(false);

            _clipboardItemsChangeSlidingTimer = new SlidingTimer
            {
                IntervalMs = STORE_INTERVAL_MS
            };

            _clipboardItemsChangeSlidingTimer.Elapsed += _clipboardItemsChangeSlidingTimer_Elapsed;
        }

        private void _clipboardItemsChangeSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            lock (_listLockHandle)
            {
                var removedIds = _removedSearchUnitIds.ToList();

                var addedSearchUnits = _addedSearchUnits.ToList();

                _removedSearchUnitIds.Clear();
                _addedSearchUnits.Clear();

                // start the long-running operation in a task to unlock the lists.
                Task.Run(() =>
                {
                    _lenghtySearchStoreOperationsMre.Reset();
                    try
                    {
                        DoLenghtySearchStoreOperations(removedIds, addedSearchUnits);
                    }
                    finally
                    {
                        _lenghtySearchStoreOperationsMre.Set();
                    }
                });
            }

        }

        private void DoLenghtySearchStoreOperations(List<string> removedIds, List<SearchUnit> addedSearchUnits)
        {
            if (removedIds.Any())
            {
                _searchProvider.DeleteSearchUnits(removedIds);
            }

            if (addedSearchUnits.Any())
            {
                _searchProvider.StoreSearchUnits(addedSearchUnits);
            }
        }

        public IEnumerable<string> FindSearchUnitIds(IEnumerable<ISearchTerm> searchTerms)
        {
            return _searchProvider.FindSearchUnitIds(searchTerms);
        }
        
        public void StoreSearchUnit(SearchUnit searchUnit)
        {
            lock (_listLockHandle)
            {
                _addedSearchUnits.Add(searchUnit);
            }

            _clipboardItemsChangeSlidingTimer.Slide();
        }

        public void DeleteSearchUnit(string unitId)
        {
            lock (_listLockHandle)
            {
                _removedSearchUnitIds.Add(unitId);
            }

            _clipboardItemsChangeSlidingTimer.Slide();
        }

        public void Dispose()
        {
            // stop the timer
            _clipboardItemsChangeSlidingTimer.Dispose();
            _lenghtySearchStoreOperationsMre.WaitOne(STORE_INTERVAL_MS * 3);

            if (_removedSearchUnitIds.Any() || _addedSearchUnits.Any())
            {
                DoLenghtySearchStoreOperations(_removedSearchUnitIds, _addedSearchUnits);
            }
        }
    }
}
