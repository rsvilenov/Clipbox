using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.PreviewData;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Cache;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
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
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Services
{
    public class PreviewGenerationService : IPreviewGenerationService, IDisposable
    {
        private const string APP_DATA_SUBDIR = "PreviewDataCache";
        private const int PREVIEW_GENERATION_TIMEOUT_MS = 6000;
        private const long PREVIEW_BYTE_ARRAY_CACHE_MAX_SIZE_BYTES = 10 * 1024 * 1024;

        private readonly IClipboardManager _clipboardManager;
        private readonly IPreviewGeneratorExtentionContainer _clipboardFormatPreviewExtContainer;
        private readonly ISettingsService _settingsService;

        private readonly ConcurrentDictionary<string, PreviewDimensionInfo> _previewDimensionsDictionary;

        private readonly SimpleDispatcher _bgGenerationDispatcher;
        private readonly SimpleDispatcher _storeToDiskDispatcher;

        private readonly ConcurrentDictionary<string, ManualResetEvent> _resetEventsForClipboardItemIds;
        private readonly ConcurrentDictionary<string, bool> _clipboardItemIdsWithCompletedTasks;

        private readonly ByteArrayCache _byteArrayCache;

        public PreviewGenerationService(
            IClipboardManager clipboardManager,
            IPreviewGeneratorExtentionContainer clipboardFormatPreviewExtContainer, 
            ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _clipboardFormatPreviewExtContainer = clipboardFormatPreviewExtContainer;
            _settingsService = settingsService;

            _previewDimensionsDictionary = LoadStoredOrInitializePreviewDimensionsInfo();

            _bgGenerationDispatcher = new SimpleDispatcher();
            _storeToDiskDispatcher = new SimpleDispatcher();

            _resetEventsForClipboardItemIds = new ConcurrentDictionary<string, ManualResetEvent>();
            _clipboardItemIdsWithCompletedTasks = new ConcurrentDictionary<string, bool>();

            _byteArrayCache = new ByteArrayCache(PREVIEW_BYTE_ARRAY_CACHE_MAX_SIZE_BYTES, compressData: true);

            _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged += _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved += _clipboardManager_ItemRemoved;
        }

        private void _clipboardManager_ItemRemoved(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            _previewDimensionsDictionary.TryRemove(e.ItemId, out PreviewDimensionInfo pdi);

            // remove the info for this item from cache/stored preview data
            DeleteStoredPreviewDataTransferObject(e.ItemId);
        }

        private void _clipboardManager_ItemChanged(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            // redraw the preview data
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsBackgroundPreviewGenerationEnabled))
            {
                // spawn threads here
                TryEnqueuePreviewGenerationTask(e.ItemId);
            }
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsBackgroundPreviewGenerationEnabled))
            {
                // spawn threads here
                TryEnqueuePreviewGenerationTask(e.ItemId);
            }
        }

        private bool TryEnqueuePreviewGenerationTask(string clipboardItemId, MonitorDimensions monitorDimensions = null)
        {
            var mre = new ManualResetEvent(false);
            if (!_resetEventsForClipboardItemIds.TryAdd(clipboardItemId, mre))
            {
                // we already a running task
                return false;
            }
            
            // enqueue a task
            _bgGenerationDispatcher.BeginInvoke(() =>
            {
                var task = StartPreviewGenerationTask(clipboardItemId, monitorDimensions);
                
                // do not block the queue - wait in a separate task
                Task.Run(() =>
                {
                    // abandon the tasks that do not finish on time
                    task.Wait(TimeSpan.FromMilliseconds(PREVIEW_GENERATION_TIMEOUT_MS));

                    // signal the event
                    mre.Set();
                });
            });

            return true;
        }

        private Task StartPreviewGenerationTask(string clipboardItemId, MonitorDimensions monitorDimensions = null)
        {
            return Task.Run(() =>
            {
                if (monitorDimensions == null)
                {
                    MonitorUtils.DisplayMonitor currentMonitor = MonitorUtils.GetByForegroundWindow();
                    monitorDimensions = new MonitorDimensions
                    {
                        MonitorDPI = currentMonitor.Dpi,
                        WorkAreaHeight = currentMonitor.WorkingArea.Height,
                        WorkAreaWidth = currentMonitor.WorkingArea.Width
                    };
                }

                int itemRequestedHeight = CalculateItemRequestedHeight(monitorDimensions, out bool hasHeightBeenDpiScaled);
                int itemRequestedWidth = itemRequestedHeight;
                
                IClipboardItem clipboardItem = _clipboardManager.GetClipboardItem(clipboardItemId, restoreShrinked: true);
                PreviewDataTransferObject pdto = GeneratePreviewDataTransferObject(clipboardItem, itemRequestedHeight, itemRequestedWidth);
                
                CacheGeneratedData(monitorDimensions, itemRequestedHeight, itemRequestedWidth, pdto, hasHeightBeenDpiScaled);
                
                _clipboardItemIdsWithCompletedTasks.TryAdd(clipboardItem.Id, true);
            });
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GetPreviewDataTransferObject(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions)
        {
            System.Threading.Thread.Sleep(3000);
            if (clipboardItem == null)
            {
                throw new ArgumentNullException(nameof(clipboardItem));
            }

            if (clipboardItem?.ClipboardDataObjects.Any() == false)
            {
                throw new ArgumentException($"{clipboardItem.Id} does not have any {nameof(clipboardItem.ClipboardDataObjects)}");
            }

            PreviewDataTransferObject pdto = null;
            
            // wait for the generation task if such is running
            bool hasTaskBeenRunning = WaitForPreviewGenerationTaskResult(clipboardItem, monitorDimensions, out pdto);

            // if no task existed
            if (!hasTaskBeenRunning
                // and if we could not load any saved data
                && !TryGetStoredPreviewDataTransferObject(clipboardItem.Id, monitorDimensions, out pdto))
            { 
                // schedule a preview generation task if it is not sheduled already
                TryEnqueuePreviewGenerationTask(clipboardItem.Id, monitorDimensions);
                // and wait for its completion
                WaitForPreviewGenerationTaskResult(clipboardItem, monitorDimensions, out pdto);
                // if we do not have a preview by now, abandon
            }

            if (pdto == null)
            {
                pdto = new PreviewDataTransferObject
                {
                    ClipboardItemId = clipboardItem.Id
                };

                pdto.PreviewItems.Add(new PreviewItemTransferObject
                {

                });
                //throw new InvalidOperationException($"Preview items generation failed for id {clipboardItem.Id}.");
            }
            
            if (!pdto.PreviewItems.Any())
            {
                throw new InvalidOperationException($"Preview items generation failed for id {clipboardItem.Id}. " +
                    $"Clipboard formats: {string.Join("; ", clipboardItem.ClipboardDataObjects.Select(cdo => cdo.ClipboardFormatName))}");
            }

            return pdto;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="clipboardItem"></param>
        /// <param name="monitorDimensions"></param>
        /// <param name="pdto"></param>
        /// <returns>True if there was a task, regardless of wheter it has completed or not; False if there were no task at all.</returns>
        private bool WaitForPreviewGenerationTaskResult(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions, out PreviewDataTransferObject pdto)
        {
            pdto = null;
            
            if (_resetEventsForClipboardItemIds.TryRemove(clipboardItem.Id, out ManualResetEvent mre))
            {
                // wait for the abandon timeout of the task to pass
                mre.WaitOne();
                mre.Dispose();

                // TODO: lock the code above up to the point where the new task is added in the dictionary,
                // so that no duplicate tasks are scheduled.

                // check if the task has actually completed
                if (_clipboardItemIdsWithCompletedTasks.TryRemove(clipboardItem.Id, out bool isFinished) && isFinished)
                {
                    // if the task is completed, then there should be saved preview data; get it
                    if (!TryGetStoredPreviewDataTransferObject(clipboardItem.Id, monitorDimensions, out PreviewDataTransferObject savedPdto))
                    {
                        throw new InvalidOperationException("The preview generation task has finished, but there is no preview saved.");
                    }

                    pdto = savedPdto;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private void CacheGeneratedData(MonitorDimensions monitorDimensions, int itemRequestedHeight, int itemRequestedWidth, PreviewDataTransferObject pdto, bool havePreviewDimensionsBeenDpiScaled)
        {
            PreviewDimensionInfo pdInfo = GetPreviewDimensionsInfo(pdto, monitorDimensions);
            pdInfo.ArePreviewDimensionsDpiScaled = havePreviewDimensionsBeenDpiScaled;
            string clipboardItemId = pdto.ClipboardItemId;

            SavePreviewDimensionInfoToDictionary(clipboardItemId, pdInfo);

            // serialize
            byte[] serializedData = pdto.ToByteArray();

            // store in the byte array cache
            _byteArrayCache.AddOrUpdate(clipboardItemId, serializedData);

            // schedule storeing to disk
            _storeToDiskDispatcher.BeginInvoke(() =>
            {
                StorePreviewDimensionsDictionaryТoDisk();
                StoreSerializedPreviewDataTransferObjectToDisk(clipboardItemId, serializedData, itemRequestedWidth, itemRequestedHeight);
            });
        }

        private PreviewDataTransferObject GeneratePreviewDataTransferObject(IClipboardItem clipboardItem, int itemRequestedHeight, int itemRequestedWidth)
        {
            IEnumerable<string> formatGuids = clipboardItem.ClipboardDataObjects.Select(cdo => cdo.IntermediateFormatObject.FormatGuid);
            KeyValuePair<string, IPreviewGenerator>? formatGuidPreviewGeneratorPair = _clipboardFormatPreviewExtContainer.ResolveClipboardPreviewGeneratorWithHighestPriority(formatGuids.ToArray());
            
            // TODO: create a default preview handler
            if (formatGuidPreviewGeneratorPair == null)
            {
                throw new KeyNotFoundException($"Unable to find a suitable preview handler for this item.");
            }

            IPreviewGenerator previewGenerator = formatGuidPreviewGeneratorPair.Value.Value;
            IClipboardDataObject cdoForPreview = clipboardItem.ClipboardDataObjects.Single(cdo => cdo.IntermediateFormatObject.FormatGuid == formatGuidPreviewGeneratorPair.Value.Key);
            
            PreviewDataTransferObject pdto = GetPdto(itemRequestedHeight, itemRequestedWidth, previewGenerator, cdoForPreview);
            
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

        private void StoreSerializedPreviewDataTransferObjectToDisk(string clipboardItemId, byte[] serializedPdto, int itemRequestedWidth, int itemRequestedHeight)
        {
            EnsureAppDataSubDirExists();

            string fileName = $"{clipboardItemId}_{itemRequestedWidth}_{itemRequestedHeight}.pdto";

            AppDataUtils.WriteToApplicationData(Path.Combine(APP_DATA_SUBDIR, fileName), serializedPdto);
        }

        private bool TryGetStoredPreviewDataTransferObject(string clipboardItemId, MonitorDimensions monitorDimensions, out PreviewDataTransferObject pdto)
        {
            pdto = null;

            if (!_byteArrayCache.TryGet(clipboardItemId, out byte[] pdtoByteArray))
            {
                int itemRequestedHeight = CalculateItemRequestedHeight(monitorDimensions, out bool hasHeightBeenDpiScaled);
                int itemRequestedWidth = itemRequestedHeight;

                string relativeFilePath = Path.Combine(APP_DATA_SUBDIR, $"{clipboardItemId}_{itemRequestedWidth}_{itemRequestedHeight}.pdto");
                if (!AppDataUtils.CheckFileExists(relativeFilePath))
                {
                    return false;
                }

                pdtoByteArray = AppDataUtils.ReadBytesFromApplicationData(relativeFilePath);
                _byteArrayCache.AddOrUpdate(clipboardItemId, pdtoByteArray);
            }

            // deserialize
            try
            {
                pdto = PreviewDataTransferObject.FromByteArray(pdtoByteArray);
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
            if (!TryLoadStorePreviewDimensionsDictionary(out ConcurrentDictionary<string, PreviewDimensionInfo> dictionary))
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

        [HandleProcessCorruptedStateExceptions]
        private PreviewDataTransferObject GetPdto(int itemRequestedHeight, 
            int itemRequestedWidth, 
            IPreviewGenerator previewGenerator, 
            IClipboardDataObject cdoForPreview)
        {
            PreviewDataTransferObject pdto = new PreviewDataTransferObject
            {
                PreviewItems = new List<PreviewItemTransferObject>()
            };
            
            if (previewGenerator is ISinglePreviewGenerator)
            {
                ISinglePreviewGenerator singlePreviewGenerator = previewGenerator as ISinglePreviewGenerator;
                
                IPreviewData previewData = null;

                try { previewData = singlePreviewGenerator.GetPreview(cdoForPreview.IntermediateFormatObject, itemRequestedWidth, itemRequestedHeight); } // TODO: find a way to determine the preview dimentions
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(singlePreviewGenerator.GetPreview), singlePreviewGenerator.GetType(), ex); }

                PreviewItemTransferObject pito = PreviewItemTransferObject.FromPreviewData(previewData);
                pdto.PreviewItems.Add(pito);
                pdto.IsGroup = false;
            }
            else if (previewGenerator is IManyPreviewGenerator)
            {
                IManyPreviewGenerator manyPreviewGenerator = previewGenerator as IManyPreviewGenerator;

                try { manyPreviewGenerator.SetData(cdoForPreview.IntermediateFormatObject); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewGenerator.SetData), manyPreviewGenerator.GetType(), ex); }

                int itemsCount = 0;
                try { itemsCount = manyPreviewGenerator.GetItemsTotalCount(); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewGenerator.SetData), manyPreviewGenerator.GetType(), ex); }

                if (itemsCount == 0)
                {
                    throw new InvalidOperationException($"{manyPreviewGenerator.GetType().Name}.{nameof(manyPreviewGenerator.GetItemsTotalCount)} returned 0 and no exception was detected. This indicates a mistake in the preview generator extension's logic.");
                }

                int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);

                IEnumerable<IPreviewData> previewDataCollection = null;
                try { previewDataCollection = manyPreviewGenerator.GetPreview(itemRequestedWidth, itemRequestedHeight, maxPreviewItemsInGroupItem); }
                catch (Exception ex) { throw new ClipboxExtentionException(nameof(manyPreviewGenerator.GetPreview), manyPreviewGenerator.GetType(), ex); }

                if (itemsCount == 0)
                {
                    throw new InvalidOperationException($"{manyPreviewGenerator.GetType().Name}.{nameof(manyPreviewGenerator.GetPreview)} returned 0 previews and no exception was detected. This indicates a mistake in the preview generator extension's logic.");
                }

                foreach (IPreviewData previewData in previewDataCollection)
                {
                    PreviewItemTransferObject pito = PreviewItemTransferObject.FromPreviewData(previewData);
                    pdto.PreviewItems.Add(pito);
                }

                if (itemsCount > 1)
                {
                    int itemsWithNoPreview = itemsCount - pdto.PreviewItems.Count();
                    pdto.NumberOfItemsWithoutPreview = itemsWithNoPreview;
                    pdto.IsGroup = true;
                }
                else
                {
                    pdto.IsGroup = false;
                }
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

            // Dispose of the manual reset events for the tasks,
            // whose previde data has not been requested and the tasks - removed from the collection;
            // Abandon the associated tasks, if there are any.
            foreach (ManualResetEvent mre in _resetEventsForClipboardItemIds.Values)
            {
                mre.Dispose();
            }
        }
    }
}
