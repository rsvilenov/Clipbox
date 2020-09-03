using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.CloudSupport;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Exceptions;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class CloudSynchronizationService : 
        ICloudSynchronizationService, 
        IDisposable
    {
        private readonly string _cloudStorageCategoryName;

        public bool IsNetworkAvailable { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public bool IsEnabled { get; set; }

        public bool IsInErrorState { get; private set; }

        public event EventHandler NetworkAvailabilityChanged;
        public event EventHandler AuthenticationStateChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<SyncEventArgs> ItemSynced;


        private readonly IClipboardManager _clipboardManager;
        private readonly ICloudSupportExtensionContainer _extensionContainer;
        private readonly ISettingsService _settingsService;
        private readonly string _appGuid;

        private bool _isAuthenticating;
        private readonly CancellationTokenSource _syncTaskCancellationTokenSource;
        private Task _syncRunnerTask;
        private readonly SynchronizationContext _mainSyncContext;

        private CloudSupportExtension _cloudSupport;

        private string _lastError;

        public CloudSynchronizationService(IClipboardManager clipboardManager,
            ISettingsService settingsService,
            ICloudSupportExtensionContainer extensionContainer, 
            string appGuid)
        {
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;
            _extensionContainer = extensionContainer;
            _appGuid = appGuid;

            _cloudStorageCategoryName = $"{ProductConstants.PRODUCT_NAME}Clipboard";

            _mainSyncContext = SynchronizationContext.Current;

            _syncTaskCancellationTokenSource = new CancellationTokenSource();

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Task.Run(() =>
            {
                try
                {
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        IsNetworkAvailable = true;
                        Authenticate();
                    }
                }
                catch (TimeoutException)
                {
                    IsNetworkAvailable = false;
                }
                catch (Exception ex)
                {
                    var edi = ExceptionDispatchInfo.Capture(ex);
                    _mainSyncContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                }
            });

            StartSyncTask();
        }

        public string GetLastError()
        {
            return _lastError;
        }
        
        private void StartSyncTask()
        {
            CancellationToken ct = _syncTaskCancellationTokenSource.Token;
            _syncRunnerTask = Task.Run(() =>
            {
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    Thread.Sleep(100);

                    if (!(IsEnabled && IsNetworkAvailable && IsAuthenticated))
                    {
                        continue;
                    }

                    CloudServiceInfo csi = _cloudSupport.GetCloudServiceInfo();
                    if (!csi.IsHealthy)
                    {
                        _lastError = $"{csi.ServiceName} : {csi.FailureReason}";
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(new CloudSyncException(_lastError)));
                        continue;
                    }

                    try
                    {
                        PerformSynchronization();
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(new CloudSyncException("Error syncing content with cloud.", ex)));
                    }

                }

            }, _syncTaskCancellationTokenSource.Token);
        }

        private void PerformSynchronization()
        {
            // ensure the existence of the cloud target directory
            if (!_cloudSupport.CheckCategoryExists(_cloudStorageCategoryName))
            {
                _cloudSupport.CreateCategory(_cloudStorageCategoryName);
            }

            // write out app id to the cloud
            EnsureAppIdFileOnCloud();

            IEnumerable<string> allAppIds = _cloudSupport.
                ListItems(_cloudStorageCategoryName, "_app_").
                Select(file => file.Replace("_app_", "").Replace(".txt", ""));


            List<string> deletionRequests = _cloudSupport.ListItems(_cloudStorageCategoryName, "_dr_").ToList();
            if (deletionRequests.Any())
            {
                DeleteOldestDeletionRequest(deletionRequests);
            }
            

            List<string> existingCloudItems = _cloudSupport.ListItems(_cloudStorageCategoryName, $"_cso_").ToList();

            IReadOnlyList<IClipboardSyncObject> itemsForCloudSync = _clipboardManager.GetItemsForCloudSynchronization();

            foreach (IClipboardSyncObject syncObject in itemsForCloudSync)
            {
                switch (syncObject.Action)
                {
                    case SyncAction.Update:
                        UpdateRemoteSyncObject(syncObject, existingCloudItems);
                        break;
                    case SyncAction.Create:
                        CreateRemoteSyncObject(syncObject, existingCloudItems);
                        break;
                    case SyncAction.Delete:
                        DeleteRemoteSyncObject(syncObject, deletionRequests, allAppIds, existingCloudItems);
                        break;
                }

                _clipboardManager.SetItemSynchronized(syncObject.Id);
                ItemSynced?.Invoke(this, new SyncEventArgs(syncObject.Id));
            }

            DownloadNowOwnedCloudItems(existingCloudItems);
        }

        private void DownloadNowOwnedCloudItems(List<string> existingCloudItems)
        {
            IEnumerable<string> allClipboardItemIds = _clipboardManager.GetAllItemIds();
            var itemsToDownload = new List<string>();
            foreach (string cloudItem in existingCloudItems)
            {
                string cloudItemId = cloudItem.Split('_')[2];

                if (!allClipboardItemIds.Contains(cloudItemId))
                {

                }
            }
        }

        private void DeleteOldestDeletionRequest(List<string> deletionRequests)
        {
            string oldestDeletionRequest = deletionRequests.OrderBy(s => long.Parse(s.Split(new char[] { '_', '.' })[3])).First();
            DateTime dt = DateTime.ParseExact(oldestDeletionRequest.Split(new char[] { '_', '.' })[3], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            if (dt < DateTime.UtcNow.AddDays(-10))
            {
                if (_cloudSupport.CheckItemExists(_cloudStorageCategoryName, oldestDeletionRequest))
                {
                    _cloudSupport.DeleteItem(_cloudStorageCategoryName, oldestDeletionRequest);
                }

                deletionRequests.Remove(oldestDeletionRequest);
            }
        }

        private void EnsureAppIdFileOnCloud()
        {
            string appGuidFileName = $"_app_{_appGuid}.txt";
            if (!_cloudSupport.CheckItemExists(_cloudStorageCategoryName, appGuidFileName))
            {
                _cloudSupport.PutItem(_cloudStorageCategoryName, 
                    appGuidFileName, 
                    Encoding.UTF8.GetBytes(_appGuid));
            }
        }

        private void DeleteRemoteSyncObject(IClipboardSyncObject syncObject, IEnumerable<string> deletionRequests, IEnumerable<string> allAppIds, List<string> existingItems)
        {
            string cloudFileName = $"{syncObject.Id}.zip";
            string deleteRequestFileName = $"_dr_{syncObject.Id}_{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.txt";

            string fileContents = $"{_appGuid}\n";

            if (deletionRequests.Contains(deleteRequestFileName))
            {
                byte[] itemBytes = _cloudSupport.GetItem(_cloudStorageCategoryName, deleteRequestFileName);
                fileContents = Encoding.UTF8.GetString(itemBytes);

                string[] appGuids = fileContents.Split('\n');
                if (!appGuids.Contains(_appGuid))
                {
                    fileContents = $"{fileContents}\n{_appGuid}";
                }
            }
            else
            {
                DeleteExistingCloudVersionsOfTheObject(syncObject, existingItems);
            }

            bool allAppsDeletedTheFile = true;
            string[] fileContentsAppId = fileContents.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string appId in allAppIds)
            {
                if (!fileContentsAppId.Contains(appId))
                {
                    allAppsDeletedTheFile = false;
                }
            }

            if (allAppsDeletedTheFile)
            {
                if (_cloudSupport.CheckItemExists(_cloudStorageCategoryName, deleteRequestFileName))
                {
                    _cloudSupport.DeleteItem(_cloudStorageCategoryName, deleteRequestFileName);
                }
            }
            else
            {
                if (!_cloudSupport.CheckItemExists(_cloudStorageCategoryName, deleteRequestFileName))
                {
                        _cloudSupport.PutItem(_cloudStorageCategoryName, 
                            deleteRequestFileName, 
                            Encoding.UTF8.GetBytes(fileContents));
                }
            }
        }

        private void CreateRemoteSyncObject(IClipboardSyncObject syncObject, List<string> existingItems)
        {
            string cloudFileName = $"_cso_{syncObject.Id}_{syncObject.ModifiedDateUtcTicks}.zip";

            if (!_cloudSupport.CheckItemExists(_cloudStorageCategoryName, cloudFileName))
            {
                _cloudSupport.PutItem(_cloudStorageCategoryName, cloudFileName, syncObject.Data);
                existingItems.Add(cloudFileName);
            }
        }

        private void UpdateRemoteSyncObject(IClipboardSyncObject syncObject, List<string> existingItems)
        {
            DeleteExistingCloudVersionsOfTheObject(syncObject, existingItems);

            string cloudFileName = $"_cso_{syncObject.Id}_{syncObject.ModifiedDateUtcTicks}.zip";

            _cloudSupport.PutItem(_cloudStorageCategoryName, cloudFileName, syncObject.Data);

            existingItems.Add(cloudFileName);
        }

        private void DeleteExistingCloudVersionsOfTheObject(IClipboardSyncObject syncObject, List<string> existingItems)
        {
            //string newestItem = existingItems.OrderBy(s => long.Parse(s.Split(new char[] { '_', '.' })[3])).Last();
            //foreach (string oldItem in existingItems.Where(it => it != newestItem))
            //{
            //    _cloudService.DeleteFile(_cloudFolderName, oldItem);
            //}
            // long dateModifiedTicks = long.Parse(newestItem.Split(new char[] { '_', '.' })[3]);


            var ownCloudItems = existingItems.Where(item => item.StartsWith($"_cso_{syncObject.Id}"))
                .ToList(); // ToList() - create another collection, so we can safely remove items from existingItems in the foreach below

            foreach (string existingOwnItem in ownCloudItems)
            {
                _cloudSupport.DeleteItem(_cloudStorageCategoryName, existingOwnItem);
                existingItems.Remove(existingOwnItem);
            }
        }

        private void Authenticate()
        {
            _isAuthenticating = true;
            IsAuthenticated = false;
            
            AuthenticationStateChanged?.Invoke(this, new EventArgs());

            if (_cloudSupport is IAuthenticatable authenticatable)
            {
                authenticatable.InitiateAuthentication(null);
                authenticatable.Authenticated += (sender, args) =>
                {
                    IsAuthenticated = true;
                    _isAuthenticating = false;

                    AuthenticationStateChanged?.Invoke(this, new EventArgs());
                };

                authenticatable.AuthenticationFailed += (sender, args) =>
                {
                    IsAuthenticated = false;
                    _isAuthenticating = false;

                    AuthenticationStateChanged?.Invoke(this, new EventArgs());
                };
            }
            else
            {
                // if the extension does not implement IAuthenticatable,
                // then no authentication is needed, so just
                // say that the autentication is successfull
                IsAuthenticated = true;
                _isAuthenticating = false;

                AuthenticationStateChanged?.Invoke(this, new EventArgs());
            }
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            IsNetworkAvailable = e.IsAvailable;
            NetworkAvailabilityChanged?.Invoke(this, new EventArgs());
            if (IsNetworkAvailable && !_isAuthenticating)
            {
                Authenticate();
            }
        }

        public void Dispose()
        {
            // request sync task cancellation
            _syncTaskCancellationTokenSource.Cancel();

            var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher != null)
            {
                DispatcherUtils.DispatchWhile(dispatcher, () => !_syncRunnerTask.IsCompleted);
            }
            else
            {
                _syncRunnerTask.Wait();
            }

            _syncTaskCancellationTokenSource.Dispose();
        }
    }
}
