using Clipbox.Core;
using Clipbox.Core.ClipboardContent.Enums;
using Clipbox.Core.ClipboardContent.Interfaces;
using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.CloudSupport;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Exceptions;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
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
        public bool IsNetworkAvailable { get; private set; }

        public bool IsAuthenticated { get; private set; }

        public bool IsAuthenticating { get; private set; }

        public bool IsEnabled { get; set; }

        public bool IsCloudSyncEnabled { get; set; }

        public bool IsInErrorState { get; private set; }

        public event EventHandler NetworkAvailabilityChanged;
        public event EventHandler<AuthenticationStageEventArgs> AuthenticationStateChanged;

        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<SyncEventArgs> ItemSynced;


        private readonly IClipboardManager _clipboardManager;
        private readonly ICloudSupportExtensionContainer _extensionContainer;
        private readonly ISettingsService _settingsService;
        private readonly string _appGuid;


        private readonly CancellationTokenSource _syncTaskCancellationTokenSource;
        private Task _syncRunnerTask;
        private readonly SynchronizationContext _mainSyncContext;
        private readonly string _cloudStorageCategoryName;

        private CloudSupportExtension _cloudSupportExt;
        private string _lastError;
        private SemaphoreSlim _authTaskSemaphore;
        private int _numberOfAuthFailures;

        private ManualResetEvent _cloudExtensionAuthMre;


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

            _authTaskSemaphore = new SemaphoreSlim(1);

            _cloudExtensionAuthMre = new ManualResetEvent(true);

            IsEnabled = _settingsService.GetSetting<bool>(SettingsKeys.AreCloudServicesEnabled);
            IsCloudSyncEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);

            string selectedCloudSupportServiceName = _settingsService.GetSetting<string>(SettingsKeys.SelectedCloudSupportServiceName);
            if (!string.IsNullOrEmpty(selectedCloudSupportServiceName))
            {
                _cloudSupportExt = _extensionContainer.Resolve(selectedCloudSupportServiceName);
                SetUpSelectedCloudSupportExtension();
            }

            _settingsService.SettingChanged += _settingsService_SettingChanged;

            IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            if (_cloudSupportExt != null)
            {
                RequestAuthentication();
            }

            StartSyncTask();
        }

        /// <summary>
        /// Gets all the available extensions which are not disabled
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> GetAvailableExtensionTypes()
        {
            return _extensionContainer.GetRegisteredTypes();
        }

        public void RequestAuthentication()
        {
            Task.Run(() =>
            {
                try
                {
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        IsNetworkAvailable = true;
                        EnqueueAuthTask();
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
        }

        private void _settingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.SelectedCloudSupportServiceName)
            {
                StopSyncTask();
                IsAuthenticated = false;

                if (_cloudSupportExt != null)
                {
                    UnsubscribeCloudSupportExtensionEvents();
                }

                string selectedCloudSupportServiceName = _settingsService.GetSetting<string>(SettingsKeys.SelectedCloudSupportServiceName);
                if (!string.IsNullOrEmpty(selectedCloudSupportServiceName))
                {
                    _cloudSupportExt = _extensionContainer.Resolve(selectedCloudSupportServiceName);

                    SetUpSelectedCloudSupportExtension();

                    RequestAuthentication();
                    StartSyncTask();
                }
            }
            else if (e.Key == SettingsKeys.IsCloudSyncEnabled)
            {
                IsCloudSyncEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);
            }
            else if (e.Key == SettingsKeys.AreCloudServicesEnabled)
            {
                IsEnabled = _settingsService.GetSetting<bool>(SettingsKeys.AreCloudServicesEnabled);
                if (!IsEnabled)
                {
                    IsAuthenticated = false;
                }
            }
            else if (e.Key.TargetComponentType != null && typeof(CloudSupportExtension).IsAssignableFrom(e.Key.TargetComponentType))
            {
                LoadSettingsInCloudExtension();
            }
        }

        private void LoadSettingsInCloudExtension()
        {
            if (_cloudSupportExt is IConfigurationRecipient confRecipient)
            {
                foreach (ISettingKey settingKey in _settingsService.GetSettingKeys(_cloudSupportExt.GetType()))
                {
                    try { confRecipient.PutSetting(settingKey, _settingsService.GetSetting(settingKey)); }
                    catch (Exception ex) { throw new ClipboxExtensionException(nameof(confRecipient.PutSetting), _cloudSupportExt.GetType(), ex); }
                }
            }
        }

        private void SetUpSelectedCloudSupportExtension()
        {
            LoadSettingsInCloudExtension();

            if (_cloudSupportExt is ISettingChangeRequestor settingChangeRequestor)
            {
                settingChangeRequestor.RequestSettingChange += ConfRequestor_RequestSettingChange;
            }

            if (_cloudSupportExt is IAuthenticatable authenticatable)
            {
                authenticatable.AuthenticationStageChanged += Authenticatable_AuthenticationStageChanged;
            }
        }

        private void UnsubscribeCloudSupportExtensionEvents()
        {
            if (_cloudSupportExt is ISettingChangeRequestor settingChangeRequestor)
            {
                settingChangeRequestor.RequestSettingChange -= ConfRequestor_RequestSettingChange;
            }

            if (_cloudSupportExt is IAuthenticatable authenticatable)
            {
                authenticatable.AuthenticationStageChanged -= Authenticatable_AuthenticationStageChanged;
            }
        }

        private void Authenticatable_AuthenticationStageChanged(object sender, AuthenticationStageEventArgs e)
        {
            if (e.Stage == AuthenticationStage.Authenticated)
            {
                IsAuthenticated = true;
                IsAuthenticating = false;

                _cloudExtensionAuthMre.Set();

                AuthenticationStateChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticated));
            }
            else if (e.Stage == AuthenticationStage.Authenticating)
            {
                IsAuthenticated = false;
                IsAuthenticating = true;

                AuthenticationStateChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticating));
            }
            else if (e.Stage == AuthenticationStage.AuthenticationError)
            {
                IsAuthenticated = false;
                IsAuthenticating = false;

                _cloudExtensionAuthMre.Set();
                AuthenticationStateChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.AuthenticationError, e.Message));
            }
        }

        private void ConfRequestor_RequestSettingChange(object sender, RequestSettingChangeEventArgs e)
        {
            // save the setting
            _settingsService.SetSetting(e.SettingKey, e.Value);
            // update the setting in the extension
            IConfigurationRecipient ext = (IConfigurationRecipient)sender;
            ext.PutSetting(e.SettingKey, e.Value);
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

                    if (!(IsEnabled && IsNetworkAvailable && IsAuthenticated && _cloudSupportExt != null))
                    {
                        continue;
                    }

                    CloudServiceInfo csi = null;
                    try
                    {
                        csi = _cloudSupportExt.GetCloudServiceInfo();
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }

                    if (csi.Exception is TimeoutException)
                    {
                        continue;
                    }

                    if (!csi.IsHealthy)
                    {
                        _lastError = $"{csi.ServiceName} : {csi.FailureReason}";
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(new CloudSyncException(_lastError)));
                        continue;
                    }

                    try
                    {
                        if (IsCloudSyncEnabled)
                        {
                            PerformSynchronization();
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(new CloudSyncException("Error syncing content with cloud.", ex)));
                    }

                }

            }, _syncTaskCancellationTokenSource.Token);
        }

        private void StopSyncTask()
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
        }

        private void PerformSynchronization()
        {
            if (!IsNetworkAvailable)
            {
                return;
            }

            // ensure the existence of the cloud target directory
            if (!_cloudSupportExt.CheckCategoryExists(_cloudStorageCategoryName))
            {
                _cloudSupportExt.CreateCategory(_cloudStorageCategoryName);
            }

            // write out app id to the cloud
            EnsureAppIdFileOnCloud();

            IEnumerable<string> allAppIds = _cloudSupportExt.
                ListItems(_cloudStorageCategoryName, "_app_").
                Select(file => file.Replace("_app_", "").Replace(".txt", ""));


            List<string> deletionRequests = _cloudSupportExt.ListItems(_cloudStorageCategoryName, "_dr_").ToList();
            if (deletionRequests.Any())
            {
                DeleteOldestDeletionRequest(deletionRequests);
            }

            List<string> existingCloudItems = _cloudSupportExt.ListItems(_cloudStorageCategoryName, $"_cso_").ToList();

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
                    byte[] downloadedItem = _cloudSupportExt.GetItem(_cloudStorageCategoryName, cloudItem);
                    _clipboardManager.TryAddItemFromCloud(downloadedItem);
                }
            }
        }

        private void DeleteOldestDeletionRequest(List<string> deletionRequests)
        {
            string oldestDeletionRequest = deletionRequests.OrderBy(s => long.Parse(s.Split(new char[] { '_', '.' })[3])).First();
            DateTime dt = DateTime.ParseExact(oldestDeletionRequest.Split(new char[] { '_', '.' })[3], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            if (dt < DateTime.UtcNow.AddDays(-10))
            {
                if (_cloudSupportExt.CheckItemExists(_cloudStorageCategoryName, oldestDeletionRequest))
                {
                    _cloudSupportExt.DeleteItem(_cloudStorageCategoryName, oldestDeletionRequest);
                }

                deletionRequests.Remove(oldestDeletionRequest);
            }
        }

        private void EnsureAppIdFileOnCloud()
        {
            string appGuidFileName = $"_app_{_appGuid}.txt";
            if (!_cloudSupportExt.CheckItemExists(_cloudStorageCategoryName, appGuidFileName))
            {
                _cloudSupportExt.PutItem(_cloudStorageCategoryName,
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
                byte[] itemBytes = _cloudSupportExt.GetItem(_cloudStorageCategoryName, deleteRequestFileName);
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
                if (_cloudSupportExt.CheckItemExists(_cloudStorageCategoryName, deleteRequestFileName))
                {
                    _cloudSupportExt.DeleteItem(_cloudStorageCategoryName, deleteRequestFileName);
                }
            }
            else
            {
                if (!_cloudSupportExt.CheckItemExists(_cloudStorageCategoryName, deleteRequestFileName))
                {
                    _cloudSupportExt.PutItem(_cloudStorageCategoryName,
                        deleteRequestFileName,
                        Encoding.UTF8.GetBytes(fileContents));
                }
            }
        }

        private void CreateRemoteSyncObject(IClipboardSyncObject syncObject, List<string> existingItems)
        {
            string cloudFileName = $"_cso_{syncObject.Id}_{syncObject.ModifiedDateUtcTicks}.zip";

            if (!_cloudSupportExt.CheckItemExists(_cloudStorageCategoryName, cloudFileName))
            {
                _cloudSupportExt.PutItem(_cloudStorageCategoryName, cloudFileName, syncObject.Data);
                existingItems.Add(cloudFileName);
            }
        }

        private void UpdateRemoteSyncObject(IClipboardSyncObject syncObject, List<string> existingItems)
        {
            DeleteExistingCloudVersionsOfTheObject(syncObject, existingItems);

            string cloudFileName = $"_cso_{syncObject.Id}_{syncObject.ModifiedDateUtcTicks}.zip";

            _cloudSupportExt.PutItem(_cloudStorageCategoryName, cloudFileName, syncObject.Data);

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
                _cloudSupportExt.DeleteItem(_cloudStorageCategoryName, existingOwnItem);
                existingItems.Remove(existingOwnItem);
            }
        }


        private class AuthAttemptsFailException : CloudSyncException {}

        private void EnqueueAuthTask()
        {
            var syncContext = SynchronizationContext.Current;

            Task.Run(() =>
            {
                if (IsAuthenticated)
                {
                    return;
                }

                _authTaskSemaphore.Wait();
                
                try
                {
                    Authenticate();
                    if (!IsAuthenticated)
                    {
                        _numberOfAuthFailures++; 
                        if (_numberOfAuthFailures >= 3)
                        {
                            _numberOfAuthFailures = 0;
                            IsInErrorState = true;
                            _lastError = "Could not authenticate after the maximum number of attempts.";
                            ErrorOccurred?.Invoke(this, new ErrorEventArgs(new CloudSyncException(_lastError)));
                            return;
                        }

                        EnqueueAuthTask();
                    }
                    else
                    {
                        _numberOfAuthFailures = 0;
                    }
                }
                catch (Exception ex)
                {
                    _numberOfAuthFailures++;
                    if (_numberOfAuthFailures >= 3)
                    {
                        _numberOfAuthFailures = 0;
                        IsInErrorState = true;
                        _lastError = "Could not authenticate after the maximum number of attempts. : " + ex.Message;
                        ErrorOccurred?.Invoke(this, new ErrorEventArgs(new CloudSyncException(_lastError)));
                        return;
                    }

                    EnqueueAuthTask();
                }
                finally
                {
                    _authTaskSemaphore.Release();
                }
            });
        }

        private void Authenticate()
        {
            IsAuthenticating = true;
            IsAuthenticated = false;
            
            AuthenticationStateChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticating));

            if (_cloudSupportExt is IAuthenticatable authenticatable)
            {
                _cloudExtensionAuthMre.Reset();
                authenticatable.InitiateAuthentication(null);
                _cloudExtensionAuthMre.WaitOne(20000);
            }
            else
            {
                // if the extension does not implement IAuthenticatable,
                // then no authentication is needed, so just
                // say that the autentication is successfull
                IsAuthenticated = true;
                IsAuthenticating = false;

                AuthenticationStateChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticated));
            }

        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            IsNetworkAvailable = e.IsAvailable;
            NetworkAvailabilityChanged?.Invoke(this, new EventArgs());
            if (IsNetworkAvailable)
            {
                EnqueueAuthTask();
            }
        }

        public void Dispose()
        {
            StopSyncTask();
            
            if (_cloudSupportExt != null)
            {
                UnsubscribeCloudSupportExtensionEvents();
            }

            _syncTaskCancellationTokenSource.Dispose();
        }
    }
}
