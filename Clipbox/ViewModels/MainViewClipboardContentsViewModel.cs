using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clipbox.ViewModels
{
    public class MainViewClipboardContentsViewModel : ClipboardContentsViewModelBase
    {
        private readonly ISearchService _searchService;
        
        public MainViewClipboardContentsViewModel(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            ISettingsService settingsService,
            ISearchService searchService) 
                : base(clipboardManager, previewGenerationService, settingsService)
        {
            _searchService = searchService;
        }
        
        public IEnumerable<string> GetClipboardItemIds(IEnumerable<ISearchTerm> searchTerms)
        {
            if (searchTerms != null && searchTerms.Any())
            {
                return _searchService.FindClipboardItemIds(searchTerms);
            }
            else
            {
                return base.ClipboardManager.GetAllItemIds();
            }
        }
        
        public void LoadAllItems(MonitorDimensions monitorDimensions, IEnumerable<string> clipboardItemIds)
        {
            base.LoadAndWrapItems(monitorDimensions, clipboardItemIds);
        }

        public void DeleteItems(IEnumerable<string> clipboardItemIds)
        {
            // do not loop through the collection, passed as a method parameter here, 
            // as it may be the SelectedItems collection, which will change when we
            // remove the selected items and this will throw an exception...
            var itemsToRemove = base.ClipboardViewItemWrappers
                .Where(item => clipboardItemIds.Contains(item.Id))
                .ToList();
            
            foreach (var item in itemsToRemove)
            {
                // remove the item from the clipboard manager
                base.ClipboardManager.RemoveClipboardItem(item.Id);
                // remove the item from the collection, bound to the view; 
                base.ClipboardViewItemWrappers.Remove(item);
            }
        }

        ~MainViewClipboardContentsViewModel()
        {
            Debug.WriteLine("MainClipboardViewViewModel unloaded");
        }

    }


}
