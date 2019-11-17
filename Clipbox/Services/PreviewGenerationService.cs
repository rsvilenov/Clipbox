using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Cache;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Threading;
using Clipbox.Infrastructure.Threading.Enums;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Enums;
using Clipbox.Services.Exceptions;
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

namespace Clipbox.Services
{
    public class PreviewGenerationService : IPreviewGenerationService, IDisposable
    {
        private const string APP_DATA_SUBDIR = "PreviewDataCache";
        private const int PREVIEW_GENERATION_TIMEOUT_MS = 6000;
        private const long PREVIEW_BYTE_ARRAY_CACHE_MAX_SIZE_BYTES = 10 * 1024 * 1024;
        private const int MAX_SIMULTANEOUS_PREVIEW_GENERATION_TASKS = 5;

        private readonly IClipboardManager _clipboardManager;
        private readonly IPreviewGeneratorExtentionContainer _clipboardFormatPreviewExtContainer;
        private readonly ISettingsService _settingsService;

        private readonly ConcurrentDictionary<string, PreviewDimensionInfo> _previewDimensionsDictionary;

        private readonly SimpleDispatcher _bgGenerationDispatcher;
        private readonly SimpleDispatcher _storeToDiskDispatcher;

        private readonly SemaphoreSlim _maxTasksSemaphore;

        private readonly ConcurrentDictionary<string, ManualResetEvent> _mreStartedForClipboardItemIds;
        private readonly ConcurrentDictionary<string, ThreadExecutor> _threadExecutorsForClipboardItemIds;

        private readonly ByteArrayCache _byteArrayCache;

        private bool _disposeBegun;

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

            _maxTasksSemaphore = new SemaphoreSlim(MAX_SIMULTANEOUS_PREVIEW_GENERATION_TASKS);

            _threadExecutorsForClipboardItemIds = new ConcurrentDictionary<string, ThreadExecutor>();
            _mreStartedForClipboardItemIds = new ConcurrentDictionary<string, ManualResetEvent>();

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
            if (_threadExecutorsForClipboardItemIds.TryGetValue(clipboardItemId, out ThreadExecutor _))
            {
                // we already have a running thread
                return false;
            }

            ThreadExecutor te = new ThreadExecutor((cancellationToken) =>
            {
                try
                {
                    GeneratePreview(clipboardItemId, cancellationToken, monitorDimensions);
                }
                finally
                {
                    _maxTasksSemaphore.Release();
                }

            }, PREVIEW_GENERATION_TIMEOUT_MS);
            
            if (!_threadExecutorsForClipboardItemIds.TryAdd(clipboardItemId, te))
            {
                // we already have a running thread
                return false;
            }

            ManualResetEvent mre = new ManualResetEvent(false);
            if (!_mreStartedForClipboardItemIds.TryAdd(clipboardItemId, mre))
            {
                throw new InvalidOperationException("At this point there shouldn't be possible for the code to try adding a mre for an existing clipboard item id. But it just did.");
            }

            // enqueue a task
            _bgGenerationDispatcher.BeginInvoke(() =>
            {
                if (_disposeBegun)
                {
                    return;
                }
                
                _maxTasksSemaphore.Wait();

                te.Start();

                // indicate to the listeners that we have strated the thread
                mre.Set();
            });

            return true;
        }

        private void GeneratePreview(string clipboardItemId, CancellationToken cancellationToken, MonitorDimensions monitorDimensions = null)
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
            PreviewDataTransferObject pdto = GeneratePreviewDataTransferObject(clipboardItem, itemRequestedHeight, itemRequestedWidth, cancellationToken);

            CacheGeneratedData(monitorDimensions, itemRequestedHeight, itemRequestedWidth, pdto, hasHeightBeenDpiScaled);
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GetPreviewDataTransferObject(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions)
        {
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
                bool hasTheNewTaskBeenStarted = WaitForPreviewGenerationTaskResult(clipboardItem, monitorDimensions, out pdto);
                if (!hasTheNewTaskBeenStarted)
                {
                    throw new InvalidOperationException($"It seems that the newly enqueued task for clipboard item with id {clipboardItem.Id} has not been started.");
                }

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
                    // TODO: this is not entirely accurate, as the reason for failure may not be a timeout
                    PreviewGenereationStatus = PreviewGenerationStatus.Timeout
                });
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
            
            if (_threadExecutorsForClipboardItemIds.TryRemove(clipboardItem.Id, out ThreadExecutor threadExecutor))
            {
                try
                {
                    if (!_mreStartedForClipboardItemIds.TryRemove(clipboardItem.Id, out ManualResetEvent mre))
                    {
                        throw new InvalidOperationException($"No ManualResetEvent was registered for clipboard item with id {clipboardItem.Id}");
                    }

                    try
                    {
                        mre.WaitOne();
                    }
                    finally
                    {
                        mre.Dispose();
                    }

                    // wait for the abandon timeout of the task to pass
                    ExecutionStage stage = threadExecutor.WaitUntilTimeoutReached();

                    // TODO: lock the code above up to the point where the new task is added in the dictionary,
                    // so that no duplicate tasks are scheduled.

                    // check if the task has actually completed
                    if (stage == ExecutionStage.Completed)
                    {
                        // if the task is completed, then there should be saved preview data; get it
                        if (!TryGetStoredPreviewDataTransferObject(clipboardItem.Id, monitorDimensions, out PreviewDataTransferObject savedPdto))
                        {
                            throw new InvalidOperationException("The preview generation task has finished, but there is no preview saved.");
                        }

                        pdto = savedPdto;
                    }
                    else if (stage == ExecutionStage.Error)
                    {
                        try
                        {
                            Exception ex = threadExecutor.GetException();
                            throw new PreviewGenerationException($"Preview generation failed for {clipboardItem.Id}", ex);
                        }
                        catch (InvalidOperationException ioex)
                        {
                            throw new InvalidOperationException($"Wrong usage of {nameof(ThreadExecutor)} or internal error in its code.", ioex);
                        }
                    }
                }
                finally
                {
                    threadExecutor.Dispose();
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
        

        private PreviewDataTransferObject GeneratePreviewDataTransferObject(IClipboardItem clipboardItem, int itemRequestedHeight, int itemRequestedWidth, CancellationToken cancellationToken)
        {
            IEnumerable<string> formatGuids = clipboardItem.ClipboardDataObjects.Select(cdo => cdo.IntermediateFormatObject.FormatGuid);
            KeyValuePair<string, IPreviewGenerator>? formatGuidPreviewGeneratorPair = _clipboardFormatPreviewExtContainer.ResolveClipboardPreviewGeneratorWithHighestPriority(formatGuids.ToArray());

            PreviewDataTransferObject pdto;

            if (formatGuidPreviewGeneratorPair != null)
            {
                IPreviewGenerator previewGenerator = formatGuidPreviewGeneratorPair.Value.Value;
                IClipboardDataObject cdoForPreview = clipboardItem.ClipboardDataObjects.Single(cdo => cdo.IntermediateFormatObject.FormatGuid == formatGuidPreviewGeneratorPair.Value.Key);
                IIntermediateFormat intermediateFormatObjectForPreview = cdoForPreview.IntermediateFormatObject;
                
                if (previewGenerator is IHaveCancellationToken cancellableGenerator)
                {
                    cancellableGenerator.CancellationToken = cancellationToken;
                }

                if (previewGenerator is ISinglePreviewGenerator singlePreviewGenerator)
                {
                    pdto = GeneratePdtoForSingleItem(itemRequestedHeight, itemRequestedWidth, singlePreviewGenerator, intermediateFormatObjectForPreview);
                }
                else if (previewGenerator is IManyPreviewGenerator manyPreviewGenerator)
                {
                    int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);
                    pdto = GeneratePdtoForMultiItem(itemRequestedHeight, itemRequestedWidth, manyPreviewGenerator, intermediateFormatObjectForPreview, maxPreviewItemsInGroupItem);
                }
                else
                {
                    throw new InvalidOperationException($"{previewGenerator.GetType().Name} is neither {nameof(ISinglePreviewGenerator)} nor {nameof(IManyPreviewGenerator)}.");
                }

                pdto.ClipboardItemId = clipboardItem.Id;
            }
            else
            {
                pdto = new PreviewDataTransferObject
                {
                    ClipboardItemId = clipboardItem.Id
                };

                pdto.PreviewItems.Add(new PreviewItemTransferObject
                {
                    PreviewGenereationStatus = PreviewGenerationStatus.NoGeneratorFound
                });
            }

            return pdto;
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GeneratePdtoForSingleItem(int itemRequestedHeight,
            int itemRequestedWidth,
            ISinglePreviewGenerator previewGenerator,
            IIntermediateFormat intermediateFormatObject)
        {
            ISinglePreviewGenerator singlePreviewGenerator = previewGenerator as ISinglePreviewGenerator;

            IPreviewData previewData = null;

            try { previewData = singlePreviewGenerator.GetPreview(intermediateFormatObject, itemRequestedWidth, itemRequestedHeight); } // TODO: find a way to determine the preview dimentions
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(singlePreviewGenerator.GetPreview), singlePreviewGenerator.GetType(), ex); }

            PreviewItemTransferObject pito = PreviewItemTransferObject.FromPreviewData(previewData);

            PreviewDataTransferObject pdto = new PreviewDataTransferObject
            {
                PreviewItems = new List<PreviewItemTransferObject> { pito },
                IsGroup = false
            };

            return pdto;
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GeneratePdtoForMultiItem(int itemRequestedHeight,
            int itemRequestedWidth,
            IManyPreviewGenerator previewGenerator,
            IIntermediateFormat intermediateFormatObject,
            int maxPreviewItemsInGroupItem)
        {

            PreviewDataTransferObject pdto = new PreviewDataTransferObject
            {
                PreviewItems = new List<PreviewItemTransferObject>()
            };

            try { previewGenerator.SetData(intermediateFormatObject); }
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(previewGenerator.SetData), previewGenerator.GetType(), ex); }

            int itemsCount = 0;
            try { itemsCount = previewGenerator.GetItemsTotalCount(); }
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(previewGenerator.SetData), previewGenerator.GetType(), ex); }

            if (itemsCount == 0)
            {
                throw new InvalidOperationException($"{previewGenerator.GetType().Name}.{nameof(previewGenerator.GetItemsTotalCount)} returned 0 and no exception was detected. This indicates a mistake in the preview generator extension's logic.");
            }

            IEnumerable<IPreviewData> previewDataCollection = null;
            try { previewDataCollection = previewGenerator.GetPreview(itemRequestedWidth, itemRequestedHeight, maxPreviewItemsInGroupItem); }
            catch (Exception ex) { throw new ClipboxExtentionException(nameof(previewGenerator.GetPreview), previewGenerator.GetType(), ex); }

            if (itemsCount == 0)
            {
                throw new InvalidOperationException($"{previewGenerator.GetType().Name}.{nameof(previewGenerator.GetPreview)} returned 0 previews and no exception was detected. This indicates a mistake in the preview generator extension's logic.");
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
            _disposeBegun = true;

            _clipboardManager.ItemAdded -= _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged -= _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved -= _clipboardManager_ItemRemoved;

            // Dispose any thread executors. Their dispose method will wait for
            // them to finish or if the timeout is reached, it will wait for them
            // to be cancelled or aborted.
            foreach (var idTePair in _threadExecutorsForClipboardItemIds)
            {
                idTePair.Value.Dispose();
            }

            foreach (var idMrePair in _mreStartedForClipboardItemIds)
            {
                idMrePair.Value.Dispose();
            }

            // dispose the SimpleDispatcher instances. If we do not do that, they will
            // keep a thread busy forever and the application will never be able to
            // quit gracefully.
            _bgGenerationDispatcher.Dispose();
            _storeToDiskDispatcher.Dispose();

            _maxTasksSemaphore.Dispose();
        }
    }
}
