using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.ClipboardContent;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Clipbox.Services
{
    public class PreviewGenerationService : IPreviewGenerationService, IDisposable
    {
        private const string APP_DATA_SUBDIR = "PreviewDataCache";
        private const int PREVIEW_GENERATION_TIMEOUT_MS = 2000;

        private IClipboardManager _clipboardManager;
        private IClipboardFormatPreviewExtentionContainer _clipboardFormatPreviewExtContainer;
        private ISettingsService _settingsService;

        private ConcurrentDictionary<string, PreviewDimensionInfo> _previewDimensionsDictionary;

        private SimpleDispatcher _bgGenerationDispatcher;

        public PreviewGenerationService(
            IClipboardManager clipboardManager,
            IClipboardFormatPreviewExtentionContainer clipboardFormatPreviewExtContainer, 
            ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _clipboardFormatPreviewExtContainer = clipboardFormatPreviewExtContainer;
            _settingsService = settingsService;

            _previewDimensionsDictionary = LoadStoredOrInitializePreviewDimensionsInfo();

            _bgGenerationDispatcher = new SimpleDispatcher();

            _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged += _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved += _clipboardManager_ItemRemoved;
        }

        private void _clipboardManager_ItemRemoved(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            PreviewDimensionInfo pdi = null;
            _previewDimensionsDictionary.TryRemove(e.ItemId, out pdi);

            // remove the info for this item from cache/stored preview data
            DeleteStoredPreviewDataTransferObject(e.ItemId);
        }

        private void _clipboardManager_ItemChanged(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            // redraw the preview data
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsBackgroundPreviewGenerationEnabled))
            {
                // spawn threads here
                EnqueuePreviewGenerationTask(e.ItemId);
            }
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsBackgroundPreviewGenerationEnabled))
            {
                // spawn threads here
                EnqueuePreviewGenerationTask(e.ItemId);
            }
        }

        private void EnqueuePreviewGenerationTask(string clipboardItemId)
        {
            _bgGenerationDispatcher.BeginInvoke(() =>
            {
                var task = StartPreviewGenerationTask(clipboardItemId);
                // do not block the queue - abandon the tasks that do not finish on time
                task.Wait(TimeSpan.FromMilliseconds(PREVIEW_GENERATION_TIMEOUT_MS));
            });
        }

        private Task StartPreviewGenerationTask(string clipboardItemId)
        {
            return Task.Run(() =>
            {
                MonitorUtils.DisplayMonitor currentMonitor = MonitorUtils.GetByForegroundWindow();
                MonitorDimensions monitorDimensions = new MonitorDimensions
                {
                    MonitorDPI = currentMonitor.Dpi,
                    WorkAreaHeight = currentMonitor.WorkingArea.Height,
                    WorkAreaWidth = currentMonitor.WorkingArea.Width
                };

                bool hasHeightBeenDpiScaled = false;
                int itemRequestedHeight = CalculateItemRequestedHeight(monitorDimensions, out hasHeightBeenDpiScaled);
                int itemRequestedWidth = itemRequestedHeight;

                IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
                PreviewDataTransferObject pdto = GeneratePreviewDataTransferObject(clipboardItem, itemRequestedHeight, itemRequestedWidth);
                CacheGeneratedData(monitorDimensions, itemRequestedHeight, itemRequestedWidth, pdto, hasHeightBeenDpiScaled);
            });
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GetPreviewDataTransferObject(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions)
        {
            bool hasHeightBeenDpiScaled = false;
            int itemRequestedHeight = CalculateItemRequestedHeight(monitorDimensions, out hasHeightBeenDpiScaled);
            int itemRequestedWidth = itemRequestedHeight;

            PreviewDataTransferObject pdto = null;

            if (!TryGetStoredPreviewDataTransferObject(clipboardItem.Id, itemRequestedWidth, itemRequestedHeight, out pdto))
            {
                pdto = GeneratePreviewDataTransferObject(clipboardItem, itemRequestedHeight, itemRequestedWidth);
                CacheGeneratedData(monitorDimensions, itemRequestedHeight, itemRequestedWidth, pdto, hasHeightBeenDpiScaled);
            }

            return pdto;
        }

        private void CacheGeneratedData(MonitorDimensions monitorDimensions, int itemRequestedHeight, int itemRequestedWidth, PreviewDataTransferObject pdto, bool havePreviewDimensionsBeenDpiScaled)
        {
            PreviewDimensionInfo pdInfo = GetPreviewDimensionsInfo(pdto, monitorDimensions);
            pdInfo.ArePreviewDimensionsDpiScaled = havePreviewDimensionsBeenDpiScaled;

            SavePreviewDimensionInfoToDictionary(pdto.ClipboardItemId, pdInfo);

            // store to disk
            StorePreviewDimensionsDictionaryТoDisk();
            StorePreviewDataTransferObjectToDisk(pdto, itemRequestedWidth, itemRequestedHeight);
        }

        private PreviewDataTransferObject GeneratePreviewDataTransferObject(IClipboardItem clipboardItem, int itemRequestedHeight, int itemRequestedWidth)
        {
            PreviewDataTransferObject pdto = null;

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

            pdto.ClipboardItemId = clipboardItem.Id;

            return pdto;
        }

        public void DeleteStoredPreviewDataTransferObject(string clipboardItemId)
        {
            foreach (string relativeFileName in AppDataUtils.ListFiles(APP_DATA_SUBDIR))
            {
                if (relativeFileName.StartsWith(clipboardItemId))
                {
                    AppDataUtils.DeleteFile(Path.Combine(APP_DATA_SUBDIR, relativeFileName));
                }
            }
        }

        private void StorePreviewDataTransferObjectToDisk(PreviewDataTransferObject pdto, int itemRequestedWidth, int itemRequestedHeight)
        {
            // serialize
            byte[] serializedData = pdto.ToByteArray();

            EnsureAppDataSubDirExists();

            string fileName = $"{pdto.ClipboardItemId}_{itemRequestedWidth}_{itemRequestedHeight}.pdto";

            AppDataUtils.WriteToApplicationData(Path.Combine(APP_DATA_SUBDIR, fileName), serializedData);
        }

        private bool TryGetStoredPreviewDataTransferObject(string clipboardItemId, int itemRequestedWidth, int itemRequestedHeight, out PreviewDataTransferObject pdto)
        {
            pdto = null;

            string relativeFilePath = Path.Combine(APP_DATA_SUBDIR, $"{clipboardItemId}_{itemRequestedWidth}_{itemRequestedHeight}.pdto");
            if (!AppDataUtils.CheckFileExists(relativeFilePath))
            {
                return false;
            }

            byte[] fileData = AppDataUtils.ReadBytesFromApplicationData(relativeFilePath);

            // deserialize
            try
            {
                pdto = PreviewDataTransferObject.FromByteArray(fileData);
            }
            catch (SerializationException sex)
            {
                Debug.WriteLine($"pdto for {clipboardItemId} cannot be deserialized: {sex}");
                return false;
            }
            
            return true;
        }

        private void SavePreviewDimensionInfoToDictionary(string clipboardItemId, PreviewDimensionInfo pdInfo)
        {
            _previewDimensionsDictionary.AddOrUpdate(clipboardItemId, pdInfo, (key, oldValue) => pdInfo);
        }

        private void StorePreviewDimensionsDictionaryТoDisk()
        {
            // TODO: is ConcurrentDictionary thread safe during serialization?
            byte[] serializedData = null;

            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, _previewDimensionsDictionary);
                serializedData = ms.ToArray();
            }

            EnsureAppDataSubDirExists();
            
            AppDataUtils.WriteToApplicationData(Path.Combine(APP_DATA_SUBDIR, "previewDimensionsDictionary"), serializedData);
        }

        private bool TryLoadStorePreviewDimensionsDictionary(out ConcurrentDictionary<string, PreviewDimensionInfo> dictionary)
        {
            dictionary = null;

            string relativeFilePath = Path.Combine(APP_DATA_SUBDIR, "previewDimensionsDictionary");
            if (!AppDataUtils.CheckFileExists(relativeFilePath))
            {
                return false;
            }

            byte[] fileData = AppDataUtils.ReadBytesFromApplicationData(relativeFilePath);

            // deserialize
            try
            {
                using (var ms = new MemoryStream(fileData))
                {
                    var formatter = new BinaryFormatter();
                    dictionary = (ConcurrentDictionary<string, PreviewDimensionInfo>)formatter.Deserialize(ms);
                }
            }
            catch (SerializationException sex)
            {
                Debug.WriteLine($"PreviewDimensionsDictionary cannot be deserialized: {sex}");
                return false;
            }

            return true;
        }

        private static void EnsureAppDataSubDirExists()
        {
            string dirPath = Path.Combine(AppDataUtils.GetAppDataDir(throwIfNotExists: false), APP_DATA_SUBDIR);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        private ConcurrentDictionary<string, PreviewDimensionInfo> LoadStoredOrInitializePreviewDimensionsInfo()
        {
            ConcurrentDictionary<string, PreviewDimensionInfo> dictionary = null;

            if (!TryLoadStorePreviewDimensionsDictionary(out dictionary))
            {
                dictionary = new ConcurrentDictionary<string, PreviewDimensionInfo>();
            }

            return dictionary;
        }

        private PreviewDimensionInfo GetPreviewDimensionsInfo(PreviewDataTransferObject pdto, MonitorDimensions monitorDimensions)
        {
            return new PreviewDimensionInfo
            {
                MonitorDimensions = (MonitorDimensions)monitorDimensions.Clone(),
                IsViewItemGroup = pdto.IsGroup,
                PreviewDimensions = pdto.PreviewItems.Select(pd => (PreviewDimensions)pd.PreviewDimensions.Clone()).ToList(),
                NumberOfItemsWithoutPreview = pdto.NumberOfItemsWithoutPreview
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
                pdto.IsGroup = true;
                pdto.PreviewItems = GetClipboardPreviewItemsForGroup(manyPreviewHandler, itemsCount, itemRequestedWidth, itemRequestedHeight);
                int itemsWithNoPreview = itemsCount - pdto.PreviewItems.Count();
                pdto.NumberOfItemsWithoutPreview = itemsWithNoPreview;
            }
            else
            {
                throw new ClipboxExtentionException($"The preview extention returned invalid items count: {itemsCount}.", manyPreviewHandler.GetType());
            }

            return pdto;
        }

        private int CalculateItemRequestedHeight(MonitorDimensions monitorDimensions, out bool hasHeightBeenDpiScaled)
        {
            double itemHeight = monitorDimensions.WorkAreaHeight * _settingsService.GetSetting<double>(SettingsKeys.PreviewItemHeightRatio);

            if (_settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi))
            {
                double scaleFactor = monitorDimensions.MonitorDPI / 96;
                itemHeight *= scaleFactor;
                hasHeightBeenDpiScaled = true;
            }
            else
            {
                hasHeightBeenDpiScaled = false;
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
                        pito = PreviewItemTransferObject.FromFilePreviewData(previewData);
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
            PreviewDimensionInfo pdInfo = null;

            if (!_previewDimensionsDictionary.TryGetValue(clipboardItemId, out pdInfo))
            {
                previewDimensionInfo = null;
                return false;
            }

            List<PreviewDimensions> calibratedPreviewDimensions = new List<PreviewDimensions>();

            // callibrate the info
            if (pdInfo.MonitorDimensions.Equals(monitorDimensions))
            {
                calibratedPreviewDimensions = pdInfo.PreviewDimensions.Select(pdi => new PreviewDimensions
                {
                    PreviewHeight = pdi.PreviewHeight,
                    PreviewWidth = pdi.PreviewWidth
                }).ToList();
            }
            else
            { 
                // get the scale factor of the monitor for which the original preview has been generated
                double oldScaleFactor = pdInfo.MonitorDimensions.MonitorDPI / 96;
                
                double previewItemHeightRatio = double.NaN;
                double previewItemWidthRatio = double.NaN;
                foreach (PreviewDimensions pd in pdInfo.PreviewDimensions)
                {
                    double originalPreviewItemHeight = pd.PreviewHeight;
                    double originalPreviewItemWidth = pd.PreviewWidth;
                    if (pdInfo.ArePreviewDimensionsDpiScaled)
                    {
                        // descale the dimensions
                        originalPreviewItemHeight /= oldScaleFactor;
                        originalPreviewItemWidth /= oldScaleFactor;
                    }

                    // calculate the PreviewItemHeightRatio
                    if (double.IsNaN(previewItemHeightRatio))
                    {
                        previewItemHeightRatio = pdInfo.MonitorDimensions.WorkAreaHeight / originalPreviewItemHeight;
                        previewItemWidthRatio = pdInfo.MonitorDimensions.WorkAreaWidth / originalPreviewItemWidth;
                    }

                    // use this ratio to re-scale the item for the new MonitorDimensions
                    double itemNewHeight = monitorDimensions.WorkAreaHeight / previewItemHeightRatio;
                    double itemNewWidth = monitorDimensions.WorkAreaWidth / previewItemWidthRatio;

                    if (_settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi))
                    {
                        double scaleFactor = monitorDimensions.MonitorDPI / 96;
                        itemNewHeight *= scaleFactor;
                        itemNewWidth *= scaleFactor;
                    }


                    calibratedPreviewDimensions.Add(new PreviewDimensions
                    {
                        PreviewHeight = (int)itemNewHeight,
                        PreviewWidth = (int)itemNewWidth
                    });
                }
            }

            previewDimensionInfo = new CalibratedPreviewDimentionInfo
            {
                IsViewItemGroup = pdInfo.IsViewItemGroup,
                PreviewDimentions = calibratedPreviewDimensions,
                NumberOfItemsWithoutPreview = pdInfo.NumberOfItemsWithoutPreview
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
