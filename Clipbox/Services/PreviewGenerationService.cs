using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.PreviewGenerationTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Cache;
using Clipbox.Infrastructure.Encryption;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace Clipbox.Services
{
    public class PreviewGenerationService : IPreviewGenerationService, IDisposable
    {
        /// <summary>
        /// The category under which the cache objects will be persisted.
        /// </summary>
        private const string PERSISTENCE_STORE_CATEGORY = "PreviewDataCache";

        /// <summary>
        /// The key under which the preview dimension dictionary will be persisted.
        /// </summary>
        private const string PREVIEW_DIMENSION_DIC_STORE_KEY = "previewDimensionsDictionary";

        private readonly IClipboardManager _clipboardManager;
        private readonly IPreviewGeneratorExtensionContainer _clipboardFormatPreviewExtContainer;
        private readonly ISettingsService _settingsService;

        private readonly IPersistenceProvider _persistenceProvider;

        private readonly ConcurrentDictionary<string, PreviewDimensionInfo> _previewDimensionsDictionary;

        private readonly SimpleDispatcher _bgGenerationDispatcher;
        private readonly SimpleDispatcher _storeToDiskDispatcher;

        private readonly SemaphoreSlim _maxTasksSemaphore;

        private readonly ConcurrentDictionary<string, ManualResetEvent> _mreStartedForClipboardItemIds;
        private readonly ConcurrentDictionary<string, ThreadExecutor> _threadExecutorsForClipboardItemIds;

        private readonly Dictionary<string, string> _extensionSettingChecksums;
        private readonly object _checkSumLockHandle = new object();
        private readonly ManualResetEvent _checkSumsCanChangeMre;
        private readonly ManualResetEvent _checkSumsChangeFinishedMre;
        private int _previewGenerationThreadsCounter;

        private readonly ByteArrayCache _byteArrayCache;

        private readonly int _previewGenerationTimeoutMs;

        private bool _disposeBegun;

        public PreviewGenerationService(
            IClipboardManager clipboardManager,
            IPreviewGeneratorExtensionContainer clipboardFormatPreviewExtContainer, 
            ISettingsService settingsService,
            IPersistenceProvider persistenceProvider)
        {
            _clipboardManager = clipboardManager;
            _clipboardFormatPreviewExtContainer = clipboardFormatPreviewExtContainer;
            _settingsService = settingsService;
            _persistenceProvider = persistenceProvider;

            _previewDimensionsDictionary = LoadStoredOrInitializePreviewDimensionsInfo();

            // a checksum used to determine whether a preview item can be fetched
            // from the preview item cache, or it shoud be generated anew
            _extensionSettingChecksums = new Dictionary<string, string>();
            // cache the checksums of the initial settings of the extensions
            CacheExtensionSettingChecksums();

            _checkSumsCanChangeMre = new ManualResetEvent(true);
            _checkSumsChangeFinishedMre = new ManualResetEvent(true);
            // listen for setting changes and update the chekcksum dictionary on a change
            _settingsService.SettingChanged += _settingsService_SettingChanged;

            _bgGenerationDispatcher = new SimpleDispatcher();
            _storeToDiskDispatcher = new SimpleDispatcher();

            int maxSimultaneousPreviewGenerationTasks;
            if (_settingsService.GetSetting<bool>(SettingsKeys.AutoDetermineMaxSimultaneousPreviewTasks))
            {
                maxSimultaneousPreviewGenerationTasks = Environment.ProcessorCount;
            }
            else
            {
                maxSimultaneousPreviewGenerationTasks = _settingsService.GetSetting<int>(SettingsKeys.MaxSimultaneousPreviewTasks);
            }

            _maxTasksSemaphore = new SemaphoreSlim(maxSimultaneousPreviewGenerationTasks);

            _threadExecutorsForClipboardItemIds = new ConcurrentDictionary<string, ThreadExecutor>();
            _mreStartedForClipboardItemIds = new ConcurrentDictionary<string, ManualResetEvent>();

            int previewByteArrayCacheMaxSizeBytes = _settingsService.GetSetting<int>(SettingsKeys.PreviewCacheMaxSize);
            _byteArrayCache = new ByteArrayCache(previewByteArrayCacheMaxSizeBytes, compressData: true);

            _previewGenerationTimeoutMs = _settingsService.GetSetting<int>(SettingsKeys.PreviewGenerationTimeout);

            _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            _clipboardManager.ItemChanged += _clipboardManager_ItemChanged;
            _clipboardManager.ItemRemoved += _clipboardManager_ItemRemoved;
        }

        private void CacheExtensionSettingChecksums()
        {
            IEnumerable<Type> previewExtensionTypes = _clipboardFormatPreviewExtContainer
                .GetRegisteredTypes();

            foreach (Type extType in previewExtensionTypes)
            { 
                    _extensionSettingChecksums.Add(extType.AssemblyQualifiedName, 
                        GetExtensionSettingsChecksum(extType, 
                            _settingsService.GetSettingKeys(extType)));
            }

            // add a random checksum of the servie itself
            // (to be used for storing failed previews)
            _extensionSettingChecksums.Add(this.GetType().AssemblyQualifiedName,
                MD5Hash.Create(DateTime.Now.Millisecond.ToString()));
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            // If the localization culture changes, 
            // rebuild the checksums for all registered extensions
            if (e.Key == SettingsKeys.LocalizationCulture)
            {
                IEnumerable<Type> previewExtensionTypes = _clipboardFormatPreviewExtContainer
                   .GetRegisteredTypes();

                foreach (Type t in previewExtensionTypes)
                {
                    IEnumerable<ISettingKey> newSettings = _settingsService
                        .GetSettingKeys(t);

                    string checkSum = GetExtensionSettingsChecksum(t, newSettings);
                    UpdateCheckSumForExtType(t, checkSum);
                }
            }

            // If the setting is not for a particular extension, but it is a general one,
            // then update the random checksum, used as part of the persistence key for
            // those previews, which has failed for some reason (other than a timeout).
            // When such a setting changes or the app is restarted, this service will 
            // try to generate the failed previews again.
            if (e.Key == SettingsKeys.EnabledExtensions
                || e.Key == SettingsKeys.IsBackgroundPreviewGenerationEnabled
                || e.Key == SettingsKeys.MaxPreviewItemsInGroupItem
                || e.Key == SettingsKeys.MaxSimultaneousPreviewTasks
                || e.Key == SettingsKeys.PreviewCacheMaxSize
                || e.Key == SettingsKeys.PreviewGenerationTimeout
                || e.Key == SettingsKeys.PreviewItemHeightRatio
                || e.Key == SettingsKeys.ResizeAccordingToMonitorOrSystemDpi
                || e.Key == SettingsKeys.LocalizationCulture /* the failure may be due to a culture-speciffic issue */)
            {
                string checkSum = MD5Hash.Create(DateTime.Now.Millisecond.ToString());
                UpdateCheckSumForExtType(this.GetType(), checkSum);
            }

            // if the changed setting is registered for a specific extension,
            // rebuild the checksum for tht extension.
            else if (e.Key.TargetComponentType != null
                && e.Key.TargetComponentType.GetInterface(nameof(IPreviewGenerator)) != null
                && e.Key.TargetComponentType.GetInterface(nameof(IConfigurationRecipient)) != null)
            {
                
                IEnumerable<ISettingKey> newSettings = _settingsService
                    .GetSettingKeys(e.Key.TargetComponentType);

                string checkSum = GetExtensionSettingsChecksum(e.Key.TargetComponentType, newSettings);
                UpdateCheckSumForExtType(e.Key.TargetComponentType, checkSum);
            }
        }

        private void UpdateCheckSumForExtType(Type extType, string checkSum)
        {
            // stop new preview generation threads from starting
            _checkSumsChangeFinishedMre.Reset();

            // wait fot the already running threads to finish
            _checkSumsCanChangeMre.WaitOne();

            // prevent the possibility of two threads simultaneously raising the event for the same key...
            // unlikely, but...
            lock (_checkSumLockHandle)
            {
                _extensionSettingChecksums.Remove(extType.AssemblyQualifiedName);
                _extensionSettingChecksums.Add(extType.AssemblyQualifiedName, checkSum);
            }

            // allow the preview generation threads to start again
            _checkSumsChangeFinishedMre.Set();
        }

        private string GetExtensionSettingsChecksum(Type extensionType, IEnumerable<ISettingKey> settingKeys)
        {
            string cultureSetting = _settingsService.GetSetting<string>(SettingsKeys.LocalizationCulture);

            StringBuilder settingsStr = new StringBuilder(extensionType.AssemblyQualifiedName);
            settingsStr.Append(cultureSetting);

            foreach (ISettingKey key in settingKeys)
            {
                settingsStr.Append(key.Name);
                settingsStr.Append(_settingsService.GetSetting(key)?.ToString() ?? string.Empty);
            }

            return MD5Hash.Create(settingsStr.ToString());
        }

        private void _clipboardManager_ItemRemoved(object sender, ExtendedClipboard.EventArgs.ClipboardManagerEventArgs e)
        {
            _previewDimensionsDictionary.TryRemove(e.ItemId, out PreviewDimensionInfo pdi);

            // remove the info for this item from cache/stored preview data
            DeleteStoredPreviewDataTransferObject(e.ItemId);
        }

        private void _clipboardManager_ItemChanged(object sender, ExtendedClipboard.EventArgs.ItemChangedEventArgs e)
        {
            if (e.TypeOfChange == ExtendedClipboard.Enums.TypeOfItemChange.RemoveCDOs)
            {
                // redraw the preview data
                if (_settingsService.GetSetting<bool>(SettingsKeys.IsBackgroundPreviewGenerationEnabled))
                {
                    // spawn threads here
                    TryEnqueuePreviewGenerationTask(e.ItemId);
                }
            }
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ItemAddedEventArgs e)
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

            }, _previewGenerationTimeoutMs);
            
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

            // enqueue a task - we will wait inside and want to block the
            // processing queue, but not the calling thread (this method).
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
            // set the culture explicitly, otherwise the thread we are running in
            // will get created with the system culture, despite the value of DefaultThreadCurrentCulture.
            // TODO: figure out why...
            CultureInfo ci = new CultureInfo(_settingsService.GetSetting(SettingsKeys.LocalizationCulture));
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

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

            // get the clipboard item with its full contents
            if (_clipboardManager.TryGetClipboardItem(clipboardItemId, out IClipboardItem clipboardItem, restoreShrinked: true))
            {
                // generate the preview
                PreviewDataTransferObject pdto = GeneratePreviewDataTransferObject(clipboardItem,
                    itemRequestedHeight,
                    itemRequestedWidth,
                    monitorDimensions.MonitorDPI,
                    cancellationToken);

                
                if (pdto.PreviewItems.Any())
                {
                    if (pdto.PreviewItems.All(pi => pi.PreviewDimensions == null))
                    {
                        // add preview dimensions and store the item, regardless the fact thet the 
                        // preview generation failed. Do this to avoid trying to generate the preview
                        // over and over, because a stored preview is not found.
                        pdto.PreviewItems.First().PreviewDimensions = new PreviewDimensions
                        {
                            PreviewHeight = itemRequestedHeight,
                            PreviewWidth = itemRequestedWidth
                        };
                    }
                }
                else
                {
                    pdto.PreviewItems.Add(new PreviewItemTransferObject
                    {
                        PreviewGenereationStatus = PreviewGenerationStatus.Unknown,
                        // add preview dimensions and store the item, regardless the fact thet the 
                        // preview generation failed. Do this to avoid trying to generate the preview
                        // over and over, because a stored preview is not found.
                        PreviewDimensions = new PreviewDimensions 
                        {
                            PreviewHeight = itemRequestedHeight,
                            PreviewWidth = itemRequestedWidth
                        }
                    });
                }

                // if the item has timed out, do not store the data, so that the service will try to 
                // generate the preview again next time, rather than trying to load the useless data from the cache.
                if (pdto.PreviewItems.First().PreviewGenereationStatus 
                    != PreviewGenerationStatus.Timeout)
                {
                    // store the preview for faster access
                    CacheGeneratedData(monitorDimensions,
                        itemRequestedHeight,
                        itemRequestedWidth,
                        pdto,
                        hasHeightBeenDpiScaled);
                }
                
            }
        }

        public PreviewDataTransferObject GetPreviewDataTransferObject(IClipboardItem clipboardItem, MonitorDimensions monitorDimensions)
        {
            if (clipboardItem == null)
            {
                throw new ArgumentNullException(nameof(clipboardItem));
            }

            if (clipboardItem.ClipboardDataObjects == null)
            {
                throw new ArgumentNullException($"{clipboardItem.Id} has {nameof(clipboardItem.ClipboardDataObjects)} which is null");
            }

            if (!clipboardItem.ClipboardDataObjects.Any())
            {
                throw new ArgumentException($"{clipboardItem.Id} does not have any {nameof(clipboardItem.ClipboardDataObjects)}");
            }
            
            // wait if a setting change is ongoing and the
            // setting checksum dictionaries are being modified
            _checkSumsChangeFinishedMre.WaitOne();
            // increment the thread counter
            Interlocked.Increment(ref _previewGenerationThreadsCounter);

            PreviewDataTransferObject pdto = null;

            try
            {
                // wait for the generation task if such is running
                bool hasTaskBeenRunning = WaitForPreviewGenerationTaskResult(clipboardItem.Id, monitorDimensions, out pdto);

                // if no task existed
                if (!hasTaskBeenRunning
                    // and if we could not load any saved data
                    && !TryGetStoredPreviewDataTransferObject(clipboardItem.Id, monitorDimensions, out pdto))
                {
                    // schedule a preview generation task if it is not sheduled already
                    TryEnqueuePreviewGenerationTask(clipboardItem.Id, monitorDimensions);
                    // and wait for its completion
                    bool hasTheNewTaskBeenStarted = WaitForPreviewGenerationTaskResult(clipboardItem.Id, monitorDimensions, out pdto);
                    if (!hasTheNewTaskBeenStarted)
                    {
                        throw new InvalidOperationException($"It seems that the newly enqueued task for clipboard item with id {clipboardItem.Id} has not been started.");
                    }

                    // if we do not have a preview by now, abandon; the ThreadExecutor will try to
                    // cancel the thread gracefully and if the thread's code does not cooperate,
                    // it will try to abort it.
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref _previewGenerationThreadsCounter) == 0)
                {
                    _checkSumsCanChangeMre.Set();
                }
            }

            if (pdto == null)
            {
                pdto = new PreviewDataTransferObject
                {
                    ClipboardItemId = clipboardItem.Id
                };

                pdto.PreviewItems.Add(new PreviewItemTransferObject
                {
                    // if we have reached this point, the code of the preview generation
                    // extension has run too long and is now either being
                    //(or has already been) cancelled /aborted
                    PreviewGenereationStatus = PreviewGenerationStatus.Timeout
                });
            }
            else if (pdto.PreviewItems == null || !pdto.PreviewItems.Any())
            {
                throw new InvalidOperationException($"Preview items generation failed for id {clipboardItem.Id}. No preview items were generated. " +
                    $"Clipboard formats: {string.Join("; ", clipboardItem.ClipboardDataObjects.Select(cdo => cdo.ClipboardFormatName))}");
            }

            return pdto;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="clipboardItemId"></param>
        /// <param name="monitorDimensions"></param>
        /// <param name="pdto"></param>
        /// <returns>True if there was a task, regardless of wheter it has completed or not; False if there were no task at all.</returns>
        private bool WaitForPreviewGenerationTaskResult(string clipboardItemId, MonitorDimensions monitorDimensions, out PreviewDataTransferObject pdto)
        {
            pdto = null;
            
            if (_threadExecutorsForClipboardItemIds.TryRemove(clipboardItemId, out ThreadExecutor threadExecutor))
            {
                try
                {
                    if (!_mreStartedForClipboardItemIds.TryRemove(clipboardItemId, out ManualResetEvent mre))
                    {
                        throw new InvalidOperationException($"No ManualResetEvent was registered for clipboard item with id {clipboardItemId}");
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

                    // check if the task has actually completed
                    if (stage == ExecutionStage.Completed)
                    {
                        // if the task is completed, then there should be saved preview data; get it
                        if (TryGetStoredPreviewDataTransferObject(clipboardItemId, monitorDimensions, out PreviewDataTransferObject savedPdto))
                        {
                            pdto = savedPdto;
                        }
                        else
                        {
                            pdto = new PreviewDataTransferObject
                            {
                                ClipboardItemId = clipboardItemId
                            };

                            pdto.PreviewItems.Add(new PreviewItemTransferObject
                            {
                                // TODO: be more precise here
                                PreviewGenereationStatus = PreviewGenerationStatus.Unknown
                            });
                        }
                    }
                    else if (stage == ExecutionStage.Error)
                    {
                        try
                        {
                            Exception ex = threadExecutor.GetException();
                            throw new PreviewGenerationException($"Preview generation failed for {clipboardItemId}", ex);
                        }
                        catch (InvalidOperationException ioex)
                        {
                            throw new InvalidOperationException($"Wrong usage of {nameof(ThreadExecutor)} or internal error in its code.", ioex);
                        }
                    }
                    else
                    {

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


            // check whether there has been a setting change, which has triggered
            // this preview generation, and if this is the case,
            // update the setting checksum dictionary with the new settings checksum

            // construct unique persistence key for the data

            string extensionSettingsChecksum;

            if (pdto.GeneratorExtensionTypeAssemblyQualifiedName == null)
            {
                pdto.GeneratorExtensionTypeAssemblyQualifiedName = this.GetType().AssemblyQualifiedName;
            }

            if (!_extensionSettingChecksums.ContainsKey(pdto.GeneratorExtensionTypeAssemblyQualifiedName))
            {
                // if we fail to get the setting checksum, then someone else has managed to mess up with our dictionaries
                throw new InvalidOperationException($"Thread synchronization error.");
            }

            extensionSettingsChecksum = _extensionSettingChecksums[pdto.GeneratorExtensionTypeAssemblyQualifiedName];

            string persistenceKey = $"{clipboardItemId}_{itemRequestedWidth}_{itemRequestedHeight}_{monitorDimensions.MonitorDPI}_{extensionSettingsChecksum}.pdto";

            // store in the byte array cache
            _byteArrayCache.AddOrUpdate(persistenceKey, serializedData);

            // schedule storeing to disk
            _storeToDiskDispatcher.BeginInvoke(() =>
            {
                StorePreviewDimensionsDictionaryТoDisk();
                StoreSerializedPreviewDataTransferObjectToDisk(clipboardItemId,
                    serializedData,
                    persistenceKey);
            });
        }
        

        private PreviewDataTransferObject GeneratePreviewDataTransferObject(IClipboardItem clipboardItem, int itemRequestedHeight, int itemRequestedWidth, double dpi, CancellationToken cancellationToken)
        {
            if (clipboardItem.IsShrinked)
            {
                throw new ArgumentException($"{nameof(clipboardItem)} is shrinked.");
            }

            IEnumerable<string> formatGuids = clipboardItem.ClipboardDataObjects
                .Where(cdo => cdo.IntermediateFormatMetadata != null)
                .Select(cdo => cdo.IntermediateFormatMetadata.FormatGuid);

            if (!formatGuids.Any())
            {
                throw new InvalidOperationException($"No clipboard formats found in the clipboard item with id {clipboardItem.Id}");
            }

            IDictionary<string, IPreviewGenerator> previewGenerators = 
                _clipboardFormatPreviewExtContainer.ResolveOrderedByPriorityDesc(formatGuids.ToArray());

            PreviewDataTransferObject pdto = null;
            
            if (previewGenerators.Any())
            {
                foreach (string formatGuid in previewGenerators.Keys)
                {
                    IPreviewGenerator previewGenerator = previewGenerators[formatGuid];

                    // load the settings
                    if (previewGenerator is IConfigurationRecipient confRecipient)
                    {
                        Type previewGeneratorType = previewGenerator.GetType();

                        foreach (ISettingKey settingKey in _settingsService.GetSettingKeys(previewGeneratorType))
                        {
                            try { confRecipient.PutSetting(settingKey, _settingsService.GetSetting(settingKey)); }
                            catch (Exception ex) { throw new ClipboxExtensionException(nameof(confRecipient.PutSetting), previewGeneratorType, ex); }
                        }
                    }

                    if (previewGenerator is ICancellablePreviewGenerator cancellableGenerator)
                    {
                        cancellableGenerator.CancellationToken = cancellationToken;
                    }
                    
                    IClipboardDataObject cdoForPreview = clipboardItem.ClipboardDataObjects.Single(cdo => cdo.IntermediateFormatMetadata.FormatGuid == formatGuid);
                    IIntermediateFormat intermediateFormatObjectForPreview = cdoForPreview.IntermediateFormatObject;

                    if (previewGenerator is ISinglePreviewGenerator singlePreviewGenerator)
                    {
                        pdto = GeneratePdtoForSingleItem(itemRequestedHeight,
                            itemRequestedWidth,
                            dpi,
                            singlePreviewGenerator,
                            intermediateFormatObjectForPreview);
                    }
                    else if (previewGenerator is IManyPreviewGenerator manyPreviewGenerator)
                    {
                        int maxPreviewItemsInGroupItem = _settingsService.GetSetting<int>(SettingsKeys.MaxPreviewItemsInGroupItem);
                        pdto = GeneratePdtoForMultiItem(itemRequestedHeight,
                            itemRequestedWidth,
                            dpi,
                            manyPreviewGenerator,
                            intermediateFormatObjectForPreview,
                            maxPreviewItemsInGroupItem);
                    }

                    if (pdto != null)
                    {
                        pdto.ClipboardItemId = clipboardItem.Id;
                        break;
                    }
                }

                if (pdto == null)
                {
                    pdto = new PreviewDataTransferObject
                    {
                        ClipboardItemId = clipboardItem.Id
                    };

                    pdto.PreviewItems.Add(new PreviewItemTransferObject
                    {
                        PreviewGenereationStatus = PreviewGenerationStatus.NoBitmapDataGenerated
                    });
                }
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
            double dpi,
            ISinglePreviewGenerator singlePreviewGenerator,
            IIntermediateFormat intermediateFormatObject)
        {
            PreviewDataTransferObject pdto = null;
            IPreviewData previewData = null;

            try { previewData = singlePreviewGenerator.GetPreview(intermediateFormatObject, new PreviewParams(itemRequestedWidth, itemRequestedHeight, dpi, true)); } // TODO: find a way to determine the preview dimentions
            catch (Exception ex) { throw new ClipboxExtensionException(nameof(singlePreviewGenerator.GetPreview), singlePreviewGenerator.GetType(), ex); }

            if (previewData != null)
            {
                PreviewItemTransferObject pito = PreviewItemTransferObject.FromPreviewData(previewData);

                pdto = new PreviewDataTransferObject
                {
                    PreviewItems = new List<PreviewItemTransferObject> { pito },
                    IsGroup = false,
                    GeneratorExtensionTypeAssemblyQualifiedName = singlePreviewGenerator.GetType().AssemblyQualifiedName
                };
            }
            
            return pdto;
        }

        [HandleProcessCorruptedStateExceptions]
        public PreviewDataTransferObject GeneratePdtoForMultiItem(int itemRequestedHeight,
            int itemRequestedWidth,
            double dpi,
            IManyPreviewGenerator manyPreviewGenerator,
            IIntermediateFormat intermediateFormatObject,
            int maxPreviewItemsInGroupItem)
        {
            PreviewDataTransferObject pdto = null;

            try { manyPreviewGenerator.SetData(intermediateFormatObject); }
            catch (Exception ex) { throw new ClipboxExtensionException(nameof(manyPreviewGenerator.SetData), manyPreviewGenerator.GetType(), ex); }

            int itemsCount = 0;
            try { itemsCount = manyPreviewGenerator.GetItemsTotalCount(); }
            catch (Exception ex) { throw new ClipboxExtensionException(nameof(manyPreviewGenerator.SetData), manyPreviewGenerator.GetType(), ex); }

            if (itemsCount == 0)
            {
                throw new InvalidOperationException($"{manyPreviewGenerator.GetType().Name}.{nameof(manyPreviewGenerator.GetItemsTotalCount)} returned 0 and no exception was detected. This indicates a mistake in the preview generator extension's logic.");
            }

            IEnumerable<IPreviewData> previewDataCollection = null;
            try { previewDataCollection = manyPreviewGenerator.GetPreview(new PreviewParams(itemRequestedWidth, itemRequestedHeight, dpi, true), maxPreviewItemsInGroupItem); }
            catch (Exception ex) { throw new ClipboxExtensionException(nameof(manyPreviewGenerator.GetPreview), manyPreviewGenerator.GetType(), ex); }

            if (previewDataCollection != null
                && previewDataCollection.Any())
            {
                if (itemsCount == 0)
                {
                    throw new InvalidOperationException($"{manyPreviewGenerator.GetType().Name}.{nameof(manyPreviewGenerator.GetPreview)} returned 0 previews and no exception was detected. This indicates a mistake in the preview generator extension's logic.");
                }

                pdto = new PreviewDataTransferObject
                {
                    PreviewItems = new List<PreviewItemTransferObject>()
                };

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

                pdto.GeneratorExtensionTypeAssemblyQualifiedName = manyPreviewGenerator.GetType().AssemblyQualifiedName;
            }

            return pdto;
        }

        public void DeleteStoredPreviewDataTransferObject(string clipboardItemId)
        {
            foreach (string persistenceKey in _persistenceProvider.ListKeys(PERSISTENCE_STORE_CATEGORY))
            {
                if (persistenceKey.StartsWith(clipboardItemId))
                {
                    _persistenceProvider.Delete(persistenceKey, PERSISTENCE_STORE_CATEGORY);
                }
            }
        }

        private void StoreSerializedPreviewDataTransferObjectToDisk(string clipboardItemId, byte[] serializedPdto, string persistenceKey)
        {
            _persistenceProvider.Write(persistenceKey, serializedPdto, PERSISTENCE_STORE_CATEGORY);
        }

        private bool TryGetStoredPreviewDataTransferObject(string clipboardItemId, MonitorDimensions monitorDimensions, out PreviewDataTransferObject pdto)
        {
            pdto = null;

            byte[] pdtoByteArray = null;

            int itemRequestedHeight = CalculateItemRequestedHeight(monitorDimensions, out bool hasHeightBeenDpiScaled);
            int itemRequestedWidth = itemRequestedHeight;

            foreach (var checkSum in _extensionSettingChecksums.Values)
            {
                string key = $"{clipboardItemId}_{itemRequestedWidth}_{itemRequestedHeight}_{monitorDimensions.MonitorDPI}_{checkSum}.pdto";

                if (_byteArrayCache.TryGet(key, out pdtoByteArray))
                {
                    break;
                }

                if (_persistenceProvider
                    .CheckIfKeyExists(key, PERSISTENCE_STORE_CATEGORY))
                {
                    pdtoByteArray = _persistenceProvider.ReadBytes(key, PERSISTENCE_STORE_CATEGORY);
                    _byteArrayCache.AddOrUpdate(key, pdtoByteArray);
                    break;
                }
            }

            if (pdtoByteArray == null)
            {
                return false;
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
            
            _persistenceProvider.Write(PREVIEW_DIMENSION_DIC_STORE_KEY, serializedData, PERSISTENCE_STORE_CATEGORY);
        }

        private bool TryLoadStorePreviewDimensionsDictionary(out ConcurrentDictionary<string, PreviewDimensionInfo> dictionary)
        {
            dictionary = null;

            if (!_persistenceProvider.CheckIfKeyExists(PREVIEW_DIMENSION_DIC_STORE_KEY, PERSISTENCE_STORE_CATEGORY))
            {
                return false;
            }

            byte[] dictionaryBytes = _persistenceProvider.ReadBytes(PREVIEW_DIMENSION_DIC_STORE_KEY, PERSISTENCE_STORE_CATEGORY);

            // deserialize
            try
            {
                using (var ms = new MemoryStream(dictionaryBytes))
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

            // Dispose the SimpleDispatcher instances. If we do not do that, they will
            // keep a thread busy forever and the application will never be able to
            // quit gracefully.
            _bgGenerationDispatcher.Dispose();
            _storeToDiskDispatcher.Dispose();

            // At this point we have waited for the completion of all tasks, so the semaphore's Release()
            // method will not throw ObjectDisposedException. So we can safely dispose of it here.
            _maxTasksSemaphore.Dispose();
        }
    }
}
