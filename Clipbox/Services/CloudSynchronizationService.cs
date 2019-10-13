using Clipbox.Core;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class CloudSynchronizationService : ICloudSynchronizationService, IDisposable
    {
        private readonly string _cloudFolderName;

        public bool IsNetworkAvailable { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public bool IsEnabled { get; set; }

        public event EventHandler NetworkAvailabilityChanged;

        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        public event EventHandler<SyncEventArgs> ItemSynced;

        private ICloudService _cloudService;

        private IClipboardManager _clipboardManager;

        private bool _isAuthenticating;

        private CancellationTokenSource _syncTaskCancellationTokenSource;

        private Task _syncRunnerTask;

        private string _appGuid;

        public CloudSynchronizationService(IClipboardManager clipboardManager, 
            ICloudService cloudService, 
            string appGuid)
        {
            _clipboardManager = clipboardManager;
            _cloudService = cloudService;
            _appGuid = appGuid;

            _cloudFolderName = $"{ProductConstants.PRODUCT_NAME}Clipboard";

            _syncTaskCancellationTokenSource = new CancellationTokenSource();

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                IsNetworkAvailable = true;
                Authenticate();
            }

            StartSyncTask();
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

                    try
                    {
                        PerformSynchronization();
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(ex));
                    }

                }

            }, _syncTaskCancellationTokenSource.Token);
        }

        private void PerformSynchronization()
        {
            // ensure the existence of the cloud target directory
            if (!_cloudService.CheckFolderExists(_cloudFolderName))
            {
                _cloudService.CreateFolder(_cloudFolderName);
            }

            // write out app id to the cloud
            EnsureAppIdFileOnCloud();

            IEnumerable<string> allAppIds = _cloudService.
                ListFiles(_cloudFolderName, "_app_").
                Select(file => file.Replace("_app_", "").Replace(".txt", ""));


            List<string> deletionRequests = _cloudService.ListFiles(_cloudFolderName, "_dr_").ToList();
            if (deletionRequests.Any())
            {
                DeleteOldestDeletionRequest(deletionRequests);
            }
            

            List<string> existingCloudItems = _cloudService.ListFiles(_cloudFolderName, $"_cso_").ToList();

            IReadOnlyList<ClipboardSyncObject> itemsForCloudSync = _clipboardManager.GetItemsForCloudSynchronization();

            foreach (ClipboardSyncObject syncObject in itemsForCloudSync)
            {
                switch (syncObject.Action)
                {
                    case ClipboardSyncObject.SyncAction.Update:
                        UpdateRemoteSyncObject(syncObject, existingCloudItems);
                        break;
                    case ClipboardSyncObject.SyncAction.Create:
                        CreateRemoteSyncObject(syncObject, existingCloudItems);
                        break;
                    case ClipboardSyncObject.SyncAction.Delete:
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
                if (_cloudService.CheckFileExists(_cloudFolderName, oldestDeletionRequest))
                {
                    _cloudService.DeleteFile(_cloudFolderName, oldestDeletionRequest);
                }

                deletionRequests.Remove(oldestDeletionRequest);
            }
        }

        private void EnsureAppIdFileOnCloud()
        {
            string appGuidFileName = $"_app_{_appGuid}.txt";
            if (!_cloudService.CheckFileExists(_cloudFolderName, appGuidFileName))
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(_appGuid)))
                {
                    _cloudService.UploadFile(_cloudFolderName, appGuidFileName, ms);
                }
            }
        }

        private void DeleteRemoteSyncObject(ClipboardSyncObject syncObject, IEnumerable<string> deletionRequests, IEnumerable<string> allAppIds, List<string> existingItems)
        {
            string cloudFileName = $"{syncObject.Id}.zip";
            string deleteRequestFileName = $"_dr_{syncObject.Id}_{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.txt";
            
            string fileContents = $"{_appGuid}\n";

            if (deletionRequests.Contains(deleteRequestFileName))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    _cloudService.DownloadFile(_cloudFolderName, deleteRequestFileName, ms);
                    fileContents = Encoding.UTF8.GetString(ms.ToArray());
                }

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
                if (_cloudService.CheckFileExists(_cloudFolderName, deleteRequestFileName))
                {
                    _cloudService.DeleteFile(_cloudFolderName, deleteRequestFileName);
                }
            }
            else
            {
                if (!_cloudService.CheckFileExists(_cloudFolderName, deleteRequestFileName))
                {
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContents)))
                    {
                        _cloudService.UploadFile(_cloudFolderName, deleteRequestFileName, ms);
                    }
                }
            }
        }

        private void CreateRemoteSyncObject(ClipboardSyncObject syncObject, List<string> existingItems)
        {
            string cloudFileName = $"_cso_{syncObject.Id}_{syncObject.ModifiedDateUtcTicks}.zip";

            if (!_cloudService.CheckFileExists(_cloudFolderName, cloudFileName))
            {
                using (MemoryStream ms = new MemoryStream(syncObject.Data))
                {
                    _cloudService.UploadFile(_cloudFolderName, cloudFileName, ms);
                }

                existingItems.Add(cloudFileName);
            }
        }

        private void UpdateRemoteSyncObject(ClipboardSyncObject syncObject, List<string> existingItems)
        {
            DeleteExistingCloudVersionsOfTheObject(syncObject, existingItems);

            string cloudFileName = $"_cso_{syncObject.Id}_{syncObject.ModifiedDateUtcTicks}.zip";

            using (MemoryStream ms = new MemoryStream(syncObject.Data))
            {
                _cloudService.UploadFile(_cloudFolderName, cloudFileName, ms);
            }

            existingItems.Add(cloudFileName);
        }

        private void DeleteExistingCloudVersionsOfTheObject(ClipboardSyncObject syncObject, List<string> existingItems)
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
                _cloudService.DeleteFile(_cloudFolderName, existingOwnItem);
                existingItems.Remove(existingOwnItem);
            }
        }

        private void Authenticate()
        {
            _isAuthenticating = true;
            IsAuthenticated = false;

            var authService = (IAuthenticatable)_cloudService;
            authService.InitiateAuthentication(null);
            authService.Authenticated += (sender, args) =>
            {
                IsAuthenticated = true;
                _isAuthenticating = false;
            };

            authService.AuthenticationFailed += (sender, args) =>
            {
                IsAuthenticated = false;
                _isAuthenticating = false;
            };
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
