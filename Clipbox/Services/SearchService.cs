using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes;
using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Services
{
    public class SearchService : ISearchService, IDisposable
    {
        private readonly ISearchSupportExtensionContainer _extensionContainer;
        private readonly IClipboardManager _clipboardManager;
        private readonly ISearchProvider _searchProvider;

        private readonly SlidingTimer _clipboardItemsChangeSlidingTimer;

        private readonly List<string> _clipboardItemIdsScheduledForRemoval;
        private readonly List<string> _clipboardItemIdsScheduledForAdd;

        private readonly object _listLockHandle = new object();

        private const int STORE_INTERVAL_MS = 3000;
        
        private ManualResetEvent _pendingModifyOperationsMre;

        private HashSet<string> _allClipboardItemIds;

        public SearchService(ISearchSupportExtensionContainer extensionContainer,
            IClipboardManager clipboardManager, 
            ISearchProvider searchProvider)
        {
            _extensionContainer = extensionContainer;
            _searchProvider = searchProvider;
            _clipboardManager = clipboardManager;
            
            _clipboardItemIdsScheduledForRemoval = new List<string>();
            _clipboardItemIdsScheduledForAdd = new List<string>();

            _allClipboardItemIds = new HashSet<string>(_clipboardManager.GetAllItemIds());

            _pendingModifyOperationsMre = new ManualResetEvent(false);

            _clipboardItemsChangeSlidingTimer = new SlidingTimer
            {
                IntervalMs = STORE_INTERVAL_MS
            };

            _clipboardItemsChangeSlidingTimer.Elapsed += _clipboardItemsChangeSlidingTimer_Elapsed;

            _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged += _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved += _clipboardManager_ItemRemoved;
        }

        private void _clipboardManager_ItemRemoved(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            lock (_listLockHandle)
            {
                _clipboardItemIdsScheduledForRemoval.Add(e.ItemId);
                _allClipboardItemIds.Remove(e.ItemId);
            }
            
            _clipboardItemsChangeSlidingTimer.Slide();
        }

        private void _clipboardManager_ItemChanged(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            lock (_listLockHandle)
            {
                _clipboardItemIdsScheduledForRemoval.Add(e.ItemId);
                _clipboardItemIdsScheduledForAdd.Add(e.ItemId);
            }

            _clipboardItemsChangeSlidingTimer.Slide();
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            lock (_listLockHandle)
            {
                _clipboardItemIdsScheduledForAdd.Add(e.ItemId);
                _allClipboardItemIds.Add(e.ItemId);
            }
            
            _clipboardItemsChangeSlidingTimer.Slide();
        }

        private void _clipboardItemsChangeSlidingTimer_Elapsed(object sender, EventArgs e)
        {
            lock (_listLockHandle)
            {
                var idsToRemove = _clipboardItemIdsScheduledForRemoval.ToList();
                var idsToAdd = _clipboardItemIdsScheduledForAdd.ToList();

                // first reset the mre and then clear the lists so that the 
                // dispose waits for us...
                _pendingModifyOperationsMre.Reset();

                _clipboardItemIdsScheduledForRemoval.Clear();
                _clipboardItemIdsScheduledForAdd.Clear();
                
                // start the long-running operation in a task to unlock the lists.
                Task.Run(() =>
                {
                    try
                    {
                        DoLenghtySearchStoreOperations(idsToRemove, idsToAdd);
                    }
                    finally
                    {
                        _pendingModifyOperationsMre.Set();
                    }
                });
            }

        }

        private void DoLenghtySearchStoreOperations(List<string> removedIds, List<string> addedIds)
        {
            if (removedIds.Any())
            {
                _searchProvider.DeleteSearchUnits(removedIds);
            }

            if (addedIds.Any())
            {
                _searchProvider.StoreSearchUnits(addedIds.Select(id => GenerateSearchUnit(id)));
            }
        }

        public IEnumerable<string> FindClipboardItemIds(IEnumerable<ISearchTerm> searchTerms)
        {
            return _searchProvider.FindSearchUnitIds(searchTerms)
                // prevent returning of nonexistant ids due to outdated/corrupt index
                .Where(id => _allClipboardItemIds.Contains(id));
        }

        private SearchUnit GenerateSearchUnit(string clipboardItemId)
        {
            IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId);

            List<ISearchTerm> searchTerms = new List<ISearchTerm>();

            var contentTypeSearchTerm = new ClipboardContentTypeSearchTerm(clipboardItem
                .ClipboardDataObjects
                .Select(cdo => cdo.ContentClass)
                .Distinct());

            searchTerms.Add(contentTypeSearchTerm);

            var textFieldsList = new List<string>();
            
            foreach (IClipboardDataObject cdo in clipboardItem.ClipboardDataObjects)
            {
                SearchSupportExtension ext = _extensionContainer.ResolveSearchSupport(cdo.Format);
                if (ext != null)
                {
                    string searchText = ext.GetSearchTextData(cdo.OriginalData);

                    textFieldsList.Add(searchText);
                }
            }

            var textSearchTerm = new TextSearchTerm(textFieldsList);
            searchTerms.Add(textSearchTerm);

            var sourceApplicationNameSearchTerm = new SourceApplicationNameSearchTerm(new List<string>
            {
                Path.GetFileNameWithoutExtension(clipboardItem.Source.SourceWindowInfo.ApplicationPath)
            });
            searchTerms.Add(sourceApplicationNameSearchTerm);

            var sourceWindowCaptionSearchTerm = new SearchTermSourceWindowCaption(new List<string>
            {
                clipboardItem.Source.SourceWindowInfo.Caption
            });
            searchTerms.Add(sourceWindowCaptionSearchTerm);

            var sourceMachineNameSearchTerm = new SourceMachineNameSearchTerm(new List<string>
            {
                clipboardItem.Source.MachineName
            });
            searchTerms.Add(sourceMachineNameSearchTerm);


            return new SearchUnit(clipboardItem.Id, searchTerms);
        }

        public void Dispose()
        {
            // stop the timer
            _clipboardItemsChangeSlidingTimer.Dispose();
            _pendingModifyOperationsMre.WaitOne(STORE_INTERVAL_MS * 3);

            if (_clipboardItemIdsScheduledForAdd.Any() || _clipboardItemIdsScheduledForRemoval.Any())
            {
                DoLenghtySearchStoreOperations(_clipboardItemIdsScheduledForRemoval, _clipboardItemIdsScheduledForAdd);
            }
        }
    }
}
