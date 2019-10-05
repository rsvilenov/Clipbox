using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Search;
using Clipbox.Core.Search.SearchTerms;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clipbox
{
    public class IntegrationController : IDisposable
    {
        private readonly IPreviewGenerationService _previewGenerationService;
        private readonly ISearchService _searchService;
        private readonly IClipboardManager _clipboardManager;
        
        public IntegrationController(
            IPreviewGenerationService previewGenerationService,
            ISearchService searchService,
            IClipboardManager clipboardManager)
        {
            _previewGenerationService = previewGenerationService;
            _searchService = searchService;

            _clipboardManager = clipboardManager;
            
            _clipboardManager.ItemRemoved += _clipboardManager_ItemRemoved;

            _previewGenerationService.PreviewGenerated += _previewGenerationService_PreviewGenerated;
        }

        private void _clipboardManager_ItemRemoved(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            _searchService.DeleteSearchUnit(e.ItemId);
        }

        private void _previewGenerationService_PreviewGenerated(object sender, Services.EventArgsClasses.PreviewGenerationEventArgs e)
        {
            _searchService.StoreSearchUnit(GenerateSearchUnit(e.ClipboardItem, e.PreviewDataTransferObject));
        }

        private SearchUnit GenerateSearchUnit(IClipboardItem clipboardItem, PreviewDataTransferObject pdto)
        {
            List<ISearchTerm> searchTerms = new List<ISearchTerm>();

            var contentTypeSearchTerm = new ClipboardContentType(clipboardItem
                .ClipboardDataObjects
                .Select(cdo => cdo.ContentClass)
                .Distinct());

            searchTerms.Add(contentTypeSearchTerm);

            var textFieldsList = new List<string>();
            foreach (PreviewItemTransferObject pito in pdto.PreviewItems)
            {
                if (!string.IsNullOrEmpty(pito.Description))
                {
                    textFieldsList.Add(pito.Description);
                }

                if (!string.IsNullOrEmpty(pito.Text))
                {
                    textFieldsList.Add(pito.Text);
                }
            }

            var textSearchTerm = new TextFields(textFieldsList);
            searchTerms.Add(textSearchTerm);

            var sourceApplicationNameSearchTerm = new SourceApplicationName(new List<string>
            {
                Path.GetFileNameWithoutExtension(clipboardItem.Source.SourceWindowInfo.ApplicationPath)
            });
            searchTerms.Add(sourceApplicationNameSearchTerm);

            var sourceWindowCaptionSearchTerm = new SourceWindowCaption(new List<string>
            {
                clipboardItem.Source.SourceWindowInfo.Caption
            });
            searchTerms.Add(sourceWindowCaptionSearchTerm);

            var sourceMachineNameSearchTerm = new SourceMachineName(new List<string>
            {
                clipboardItem.Source.MachineName
            });
            searchTerms.Add(sourceMachineNameSearchTerm);


            return new SearchUnit(clipboardItem.Id, searchTerms);
        }

        public void Dispose()
        {
            _clipboardManager.ItemRemoved -= _clipboardManager_ItemRemoved;
            _previewGenerationService.PreviewGenerated -= _previewGenerationService_PreviewGenerated;
        }
    }
}
