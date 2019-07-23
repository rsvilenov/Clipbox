using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Clipbox.Services
{
    public class PreviewGenerationService : IPreviewGenerationService, IDisposable
    {
        private IClipboardManager _clipboardManager;
        private IClipboardFormatPreviewExtentionContainer _clipboardFormatPreviewExtContainer;
        private ISettingsService _settingsService;

        private Dictionary<string, PreviewDimensionInfo> _previewDimensionsDictionary;
        
        public PreviewGenerationService(
            IClipboardManager clipboardManager,
            IClipboardFormatPreviewExtentionContainer clipboardFormatPreviewExtContainer, 
            ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _clipboardFormatPreviewExtContainer = clipboardFormatPreviewExtContainer;
            _settingsService = settingsService;

            _previewDimensionsDictionary = LoadStoredOrInitializePreviewDimensionsInfo();

            _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged += _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved += _clipboardManager_ItemRemoved;
        }

        private void _clipboardManager_ItemRemoved(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            // remove the info for this item from cache/stored preview data
        }

        private void _clipboardManager_ItemChanged(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            // redraw the preview data
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsBackgroundPreviewGenerationEnabled))
            {
                // spawn threads here
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GetPreviewDataTransferObject(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions)
        {
            int itemRequestedHeight = CalculateItemRequestedHeight(monitorDimensions);
            int itemRequestedWidth = itemRequestedHeight;

            PreviewDataTransferObject pdto = new PreviewDataTransferObject();

            IPreviewHandler previewHandler = null;
            ClipboardDataObject cdoForPreview = null;
            // get the first preview handler - it should be for the clipboard format with the highest priority
            foreach (ClipboardDataObject cdo in clipboardItem.ClipboardDataObjects)
            {
                previewHandler = _clipboardFormatPreviewExtContainer.ResolveClipboardPreviewHandler(cdo.Format);
                if (previewHandler != null)
                {
                    cdoForPreview = cdo;
                    break;
                }
            }

            // TODO: create a default preview handler
            if (previewHandler == null)
            {
                throw new FormatException($"Unable to find a suitable preview handler for this item.");
            }

            if (previewHandler is IManyPreviewHandler)
            {
                var manyPreviewHandler = previewHandler as IManyPreviewHandler;
                pdto = GetPdtoForGroup(itemRequestedHeight, itemRequestedWidth, previewHandler, cdoForPreview, manyPreviewHandler);
            }
            else
            {
                pdto = GetPdtoForSingle(itemRequestedHeight, itemRequestedWidth, previewHandler, cdoForPreview);
            }

            PreviewDimensionInfo pdInfo = GetPreviewDimensionsInfo(pdto, monitorDimensions);
            StorePreviewDimensionInfo(clipboardItem.Id, pdInfo);

            return pdto;
        }

        private void StorePreviewDimensionInfo(string clipboardItemId, PreviewDimensionInfo pdInfo)
        {
            if (_previewDimensionsDictionary.ContainsKey(clipboardItemId))
            {
                _previewDimensionsDictionary[clipboardItemId] = pdInfo;
            }
            else
            {
                _previewDimensionsDictionary.Add(clipboardItemId, pdInfo);
            }

            // store to disk
        }

        private Dictionary<string, PreviewDimensionInfo> LoadStoredOrInitializePreviewDimensionsInfo()
        {
            return new Dictionary<string, PreviewDimensionInfo>();
        }

        private PreviewDimensionInfo GetPreviewDimensionsInfo(PreviewDataTransferObject pdto, MonitorDimensions monitorDimensions)
        {
            return new PreviewDimensionInfo
            {
                MonitorDimensions = (MonitorDimensions)monitorDimensions.Clone(),
                IsViewItemGroup = pdto.IsGroup,
                PreviewDimentions = pdto.PreviewItems.Select(pd => (PreviewDimensions)pd.PreviewDimensions.Clone())
            };
        }

        private PreviewDataTransferObject GetPdtoForSingle(int itemRequestedHeight, int itemRequestedWidth, IPreviewHandler previewHandler, ClipboardDataObject cdoForPreview)
        {
            return new PreviewDataTransferObject
            {
                IsGroup = false,
                PreviewItems = new List<PreviewItemTransferObject>
                {
                    GetClipboardPreviewItemForSingle(cdoForPreview.OriginalData, previewHandler, itemRequestedWidth, itemRequestedHeight)
                }
            };
        }

        private PreviewDataTransferObject GetPdtoForGroup(int itemRequestedHeight, int itemRequestedWidth, IPreviewHandler previewHandler, ClipboardDataObject cdoForPreview, IManyPreviewHandler manyPreviewHandler)
        {
            PreviewDataTransferObject pdto = new PreviewDataTransferObject();

            try { manyPreviewHandler.SetData(cdoForPreview.OriginalData); }
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewHandler.SetData), manyPreviewHandler.GetType(), ex); }

            int itemsCount = 0;
            try { itemsCount = manyPreviewHandler.GetItemsTotalCount(); }
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewHandler.SetData), manyPreviewHandler.GetType(), ex); }

            if (itemsCount == 1)
            {
                pdto.IsGroup = false;
                pdto.PreviewItems = new List<PreviewItemTransferObject>
                    {
                        GetClipboardPreviewItemForSingle(cdoForPreview.OriginalData, previewHandler, itemRequestedWidth, itemRequestedHeight)
                    };
            }
            else if (itemsCount > 1)
            {
                pdto.PreviewItems = GetClipboardPreviewItemsForGroup(manyPreviewHandler, itemsCount, itemRequestedWidth, itemRequestedHeight);
                int itemsWithNoPreview = itemsCount - pdto.PreviewItems.Count();
                pdto.NMore = itemsWithNoPreview;
            }
            else
            {
                throw new ClipboxExtentionException($"The preview extention returned invalid items count: {itemsCount}.", manyPreviewHandler.GetType());
            }

            return pdto;
        }

        private int CalculateItemRequestedHeight(MonitorDimensions monitorDimensions)
        {
            double itemHeight = monitorDimensions.MonitorHeight * _settingsService.GetSetting<double>(SettingsKeys.PreviewItemHeightRatio);
            if (_settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi))
            {
                double scaleFactor = monitorDimensions.MonitorDPI / 96;
                itemHeight *= scaleFactor;
            }

            return (int)itemHeight;
        }

        [HandleProcessCorruptedStateExceptions]
        private PreviewItemTransferObject GetClipboardPreviewItemForSingle(object data, IPreviewHandler previewHandler, int previewWidth, int previewHeight)
        {
            Debug.Assert(previewHandler != null);

            PreviewItemTransferObject pito = null;

            if (previewHandler.PreviewDataType == typeof(ImagePreviewData))
            {
                PreviewHandler<ImagePreviewData> imagePreviewHandler = (PreviewHandler<ImagePreviewData>)previewHandler;
                ImagePreviewData imagePreviewData = null;

                try { imagePreviewData = imagePreviewHandler.GetPreview(data, previewWidth, previewHeight); } // TODO: find a way to determine the preview dimentions
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(imagePreviewHandler.GetPreview), previewHandler.GetType(), ex); }

                using (imagePreviewData)
                {
                    pito = PreviewItemTransferObject.FromImagePreviewData(imagePreviewData);
                }
            }
            else if (previewHandler.PreviewDataType == typeof(FilePreviewData))
            {
                ManyPreviewHandler<FilePreviewData> filePreviewHandler = (ManyPreviewHandler<FilePreviewData>)previewHandler;
                IEnumerable<FilePreviewData> previewDataCollection = Enumerable.Empty<FilePreviewData>();

                int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);

                try { previewDataCollection = filePreviewHandler.GetPreview(previewWidth, previewHeight, maxPreviewItemsInGroupItem); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(filePreviewHandler.GetPreview), previewHandler.GetType(), ex); }

                Debug.Assert(previewDataCollection != null);
                Debug.Assert(previewDataCollection.Any());

                var filePreviewData = previewDataCollection.First();

                using (filePreviewData)
                {
                    pito = PreviewItemTransferObject.FromFilePreviewData(filePreviewData);
                };
            }
            else
            {
                throw new NotImplementedException($"{previewHandler.PreviewDataType.Name} is not supported yet.");
            }

            return pito;
        }

        [HandleProcessCorruptedStateExceptions]
        private List<PreviewItemTransferObject> GetClipboardPreviewItemsForGroup(IManyPreviewHandler previewHandler, int itemsCount, int previewWidth, int previewHeight)
        {
            Debug.Assert(previewHandler != null);

            List<PreviewItemTransferObject> pitos = new List<PreviewItemTransferObject>();

            if (previewHandler.PreviewDataType == typeof(FilePreviewData))
            {
                IEnumerable<FilePreviewData> previewDataCollection;
                var filePreviewHandler = previewHandler as ManyPreviewHandler<FilePreviewData>;

                int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);

                try { previewDataCollection = filePreviewHandler.GetPreview(previewWidth, previewHeight, maxPreviewItemsInGroupItem); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(filePreviewHandler.GetPreview), filePreviewHandler.GetType(), ex); }
                
                foreach (var previewData in previewDataCollection)
                {
                    PreviewItemTransferObject pito = null;

                    using (previewData)
                    {
                        pito = PreviewItemTransferObject.FromImagePreviewData(previewData);
                    }

                    pitos.Add(pito);
                }
            }
            else
            {
                throw new NotImplementedException($"{previewHandler.PreviewDataType.Name} is not supported yet.");
            }

            return pitos;
        }


        public bool TryGetQuickPreviewDimentionInfo(string clipboardItemId, MonitorDimensions monitorDimensions, out CalibratedPreviewDimentionInfo previewDimensionInfo)
        {
            if (!_previewDimensionsDictionary.ContainsKey(clipboardItemId))
            {
                previewDimensionInfo = null;
                return false;
            }

            PreviewDimensionInfo pdInfo = _previewDimensionsDictionary[clipboardItemId];

            // callibrate the info

            previewDimensionInfo = new CalibratedPreviewDimentionInfo
            {
                IsViewItemGroup = pdInfo.IsViewItemGroup,
            };

            return true;
        }

        public void Dispose()
        {
            _clipboardManager.ItemAdded -= _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged -= _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved -= _clipboardManager_ItemRemoved;
        }
    }
}
