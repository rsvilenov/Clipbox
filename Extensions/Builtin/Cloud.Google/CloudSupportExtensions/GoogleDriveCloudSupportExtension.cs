using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Infrastructure.CloudSupport;
using Clipbox.Infrastructure.Network;
using global::Google.Apis.Auth.OAuth2;
using global::Google.Apis.Drive.v3;
using global::Google.Apis.Json;
using global::Google.Apis.Services;
using global::Google.Apis.Util.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.Extensions.BuiltIn.Cloud.Google.CloudSupportExtensions
{
    [ClipboxExtension(
           description: "Google Drive cloud support",
           author: ProductConstants.PRODUCT_NAME,
           version: 1.0,
           MinHostVersion = 1.0
           )]
    [CloudSupport("Google Drive", "GoogleDrive.png")]
    public class GoogleDriveCloudSupportExtension : CloudSupportExtension, IAuthenticatable
    {

        #region IAuthenticatable
        public bool IsAuthenticated { get; private set; }

        public event EventHandler<AuthenticationStageEventArgs> AuthenticationStageChanged;

        public void InitiateAuthentication()
        {
            AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticating));

            Authenticate(false).ContinueWith(t =>
            {
                if (_credential != null)
                {
                    IsAuthenticated = true;
                    AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticated));
                }
            });
        }

        public void ChangeLogin()
        {
            ChangeLoginInternal();
        }

        #endregion



        private UserCredential _credential;

        private List<DriveObjectInfo> _driveObjectCache;

        private readonly EventDrivenDataStore _dataStore;
        private readonly AutoResetEvent _settingChangeWaitAutoResetEvent;
        public GoogleDriveCloudSupportExtension()
        {
            _driveObjectCache = new List<DriveObjectInfo>();

            _settingChangeWaitAutoResetEvent = new AutoResetEvent(false);

            _dataStore = new EventDrivenDataStore();
            _dataStore.FetchDataRequested += _dataStore_FetchDataRequested;
            _dataStore.DataModified += _dataStore_DataModified;
        }

        private void _dataStore_DataModified(object sender, StringDataTransferEventArgs e)
        {
            // this will request a setting change. When the new setting is applied, the method
            // OnSettingUpdated() will be called. There we will set the reset event.
            RequestSettingChange<string>(ExtensionSettingNames.GoogleDriveAuthToken, e.InData);

            // Wait for the setting to be applied...
            _settingChangeWaitAutoResetEvent.WaitOne();
        }

        protected override void OnSettingUpdated(string settingName)
        {
            if (settingName == ExtensionSettingNames.GoogleDriveAuthToken)
            {
                _settingChangeWaitAutoResetEvent.Set();
            }
            else if (settingName == ExtensionSettingNames.GoogleDriveReauthRequested && GetSetting<bool>(ExtensionSettingNames.GoogleDriveReauthRequested))
            {
                ChangeLoginInternal();
            }
        }

        private void ChangeLoginInternal()
        {
            AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticating));

            RequestSettingChange(ExtensionSettingNames.GoogleDriveReauthRequested, false);
            Authenticate(updatePermissions: true).ContinueWith(t =>
            {
                if (_credential != null)
                {
                    IsAuthenticated = true;
                    AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticated));
                }
            });
        }

        private void _dataStore_FetchDataRequested(object sender, StringDataTransferEventArgs e)
        {
            e.OutData = GetSetting<string>(ExtensionSettingNames.GoogleDriveAuthToken);
        }

        public override bool CheckCategoryExists(string categoryName)
        {
            return GetInfoForDriveFolder(categoryName) != null;
        }

        public override bool CheckItemExists(string categoryName, string itemName)
        {
            // do not throw exceptions when the folder does not exists
            // (GetInfoForDriveFile throws when the folder is not found, so check the folder first)
            return GetInfoForDriveFolder(categoryName) != null &&
                GetInfoForDriveFile(categoryName, itemName) != null;
        }

        public override void CreateCategory(string categoryName)
        {
            var folderMetadata = new global::Google.Apis.Drive.v3.Data.File()
            {
                Name = categoryName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string>()
                {
                    "appDataFolder"
                }
            };

            ExecuteAuthenticated((service) =>
            {
                var request = service.Files.Create(folderMetadata);
                request.Fields = "id";
                var folder = request.Execute();

                _driveObjectCache.Add(new DriveObjectInfo
                {
                    Id = folder.Id,
                    Name = categoryName,
                    IsFolder = true
                });
            });
        }

        public override void DeleteCategory(string categoryName)
        {
            DriveObjectInfo folderCacheObj = GetInfoForDriveFolder(categoryName);

            ExecuteAuthenticated((service) =>
            {
                var deleteRequest = service.Files.Delete(folderCacheObj.Id);
                deleteRequest.Execute();

                if (_driveObjectCache.Contains(folderCacheObj))
                {
                    _driveObjectCache.Remove(folderCacheObj);
                }
            });
        }

        public override void DeleteItem(string categoryName, string itemName)
        {
            DriveObjectInfo fileCacheObj = GetInfoForDriveFile(categoryName, itemName);
            if (fileCacheObj == null)
            {
                return;
            }

            ExecuteAuthenticated((service) =>
            {
                try 
                {
                    var deleteRequest = service.Files.Delete(fileCacheObj.Id);
                    deleteRequest.Execute(); 
                }
                catch { } // TODO: ?

                if (_driveObjectCache.Contains(fileCacheObj))
                {
                    _driveObjectCache.Remove(fileCacheObj);
                }
            });
        }

        public override CloudServiceInfo GetCloudServiceInfo()
        {
            const string url = "http://drive.google.com";
            bool isAvailable = false;
            string failureReason = string.Empty;

            try
            { 
                isAvailable = NetworkUtils.CheckIsEndpointReachable(url, 80); 
                if (!isAvailable)
                {
                    failureReason = "The cloud service is unreachable.";
                }

                return new CloudServiceInfo("Google Drive",
                    url,
                    IsAuthenticated,
                    isAvailable,
                    failureReason);
            }
            catch (Exception ex)
            {
                return new CloudServiceInfo("Google Drive",
                   url,
                   IsAuthenticated,
                   isAvailable,
                   ex);
            }
            
        }

        public override byte[] GetItem(string categoryName, string itemName)
        {
            DriveObjectInfo fileCacheObj = GetInfoForDriveFile(categoryName, itemName);

            if (fileCacheObj == null)
            {
                throw new FileNotFoundException($"Remote file {itemName} could not be found.");
            }

            byte[] contents = null;

            ExecuteAuthenticated(service =>
            {
                var request = service.Files.Get(fileCacheObj.Id);
                using (MemoryStream ms = new MemoryStream())
                {
                    request.Download(ms);
                    contents = ms.ToArray();
                }
            });

            return contents;
        }

        public override void PutItem(string categoryName, string itemName, byte[] data)
        {
            DriveObjectInfo folderCacheObj = GetInfoForDriveFolder(categoryName);
            if (folderCacheObj == null)
            {
                throw new FileNotFoundException($"Remote folder {categoryName} could not be found.");
            }

            string mimeType;
            if (Path.GetExtension(itemName) == ".zip")
            {
                mimeType = "application/zip";
            }
            else if (Path.GetExtension(itemName) == ".txt")
            {
                mimeType = "text/plain";
            }
            else
            {
                throw new NotImplementedException("Currently only zip and text files can be uploaded.");
            }

            if (Path.GetFileName(itemName) != itemName)
            {
                throw new InvalidOperationException($"Got path instead of filename: {itemName}");
            }

            var fileMetadata = new global::Google.Apis.Drive.v3.Data.File()
            {
                Name = itemName,
                Parents = new List<string>
                {
                    folderCacheObj.Id
                }
            };

            ExecuteAuthenticated(service =>
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    var uploadRequest = service.Files.Create(fileMetadata, ms, mimeType);
                    uploadRequest.Fields = "id";
                    var progress = uploadRequest.Upload();
                    if (progress.Exception != null)
                    {
                        throw progress.Exception;
                    }

                    _driveObjectCache.Add(new DriveObjectInfo
                    {
                        Id = uploadRequest.ResponseBody.Id,
                        IsFolder = false,
                        Name = itemName
                    });
                }
            });
        }

        public override IEnumerable<string> ListItems(string categoryName, string itemNamePart = "")
        {
            // There seems to be an undocumented (?) limit to the length of the "name/fullText contains" query parameter.
            if (itemNamePart?.Length > 30)
            {
                throw new ArgumentOutOfRangeException($"{nameof(itemNamePart)} cannot be more than 30 characters long.");
            }

            DriveObjectInfo folderCacheObj = GetInfoForDriveFolder(categoryName);
            if (folderCacheObj == null)
            {
                throw new FileNotFoundException($"Remote folder {categoryName} could not be found.");
            }

            // TODO: make this use the paging functionality of google drive
            // https://stackoverflow.com/questions/41572228/how-to-list-of-more-than-1000-records-from-google-drive-api-v3-in-c-sharp
            IList<global::Google.Apis.Drive.v3.Data.File> files = null;
            ExecuteAuthenticated(service =>
            {
                FilesResource.ListRequest listFolderRequest = service.Files.List();
                listFolderRequest.Fields = "files(id, name)";
                listFolderRequest.Spaces = "appDataFolder";
                listFolderRequest.Q = $"'{folderCacheObj.Id}' in parents and trashed=false ";
                if (itemNamePart != string.Empty)
                {
                    listFolderRequest.Q += $" and name contains '{itemNamePart}'";
                }

                files = listFolderRequest.Execute()
                    .Files;
            });

            var fileDriveInfos = new List<DriveObjectInfo>();
            var fileNames = new List<string>();

            foreach (var file in files)
            {
                if (!_driveObjectCache.Any(dro => dro.Id == file.Id))
                {
                    _driveObjectCache.Add(new DriveObjectInfo
                    {
                        Id = file.Id,
                        Name = file.Name,
                        IsFolder = false
                    });
                }

                fileNames.Add(file.Name);
            }

            return fileNames;
        }

        private DriveObjectInfo GetInfoForDriveFile(string driveFolderName, string driveFileName)
        {
            DriveObjectInfo folderCacheObj = _driveObjectCache.
                            Where(obj => obj.Name.Equals(driveFolderName) && obj.IsFolder).
                            SingleOrDefault();

            if (folderCacheObj == null)
            {
                folderCacheObj = GetInfoForDriveFolder(driveFolderName);
            }

            if (folderCacheObj == null)
            {
                throw new FileNotFoundException($"Remote folder {driveFolderName} could not be found");
            }

            DriveObjectInfo fileCacheObj = _driveObjectCache.
                            Where(obj => obj.Name.Equals(driveFileName) && !obj.IsFolder).
                            SingleOrDefault();

            if (fileCacheObj != null)
            {
                return fileCacheObj;
            }

            ExecuteAuthenticated(service =>
            {
                FilesResource.ListRequest listFolderRequest = service.Files.List();
                listFolderRequest.Fields = "files(id)";
                listFolderRequest.Spaces = "appDataFolder";
                listFolderRequest.Q = $"'{folderCacheObj.Id}' in parents and trashed=false and name='{driveFileName}' ";

                IList<global::Google.Apis.Drive.v3.Data.File> files = listFolderRequest.Execute()
                    .Files;

                if (files.Count == 1)
                {
                    var fileInfo = files.Single();

                    fileCacheObj = new DriveObjectInfo
                    {
                        Id = fileInfo.Id,
                        IsFolder = false,
                        Name = driveFileName
                    };
                }
                else if (files.Count > 1)
                {
                    throw new InvalidOperationException($"More than one file named  {driveFileName} was found.");
                }
            });

            if (fileCacheObj != null)
            {
                _driveObjectCache.Add(fileCacheObj);
            }

            return fileCacheObj;
        }

        private DriveObjectInfo GetInfoForDriveFolder(string driveFolderName)
        {
            DriveObjectInfo folderCacheObj = _driveObjectCache.
                            Where(obj => obj.Name.Equals(driveFolderName) && obj.IsFolder).
                            SingleOrDefault();

            if (folderCacheObj != null)
            {
                return folderCacheObj;
            }

            ExecuteAuthenticated(service =>
            {
                FilesResource.ListRequest listFolderRequest = service.Files.List();
                listFolderRequest.Fields = "files(id)";

                listFolderRequest.Spaces = "appDataFolder";
                listFolderRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{driveFolderName}' and trashed=false";

                IList<global::Google.Apis.Drive.v3.Data.File> folders = listFolderRequest.Execute()
                    .Files;

                if (folders.Count == 1)
                {
                    var folderInfo = folders.Single();

                    folderCacheObj = new DriveObjectInfo
                    {
                        Id = folderInfo.Id,
                        IsFolder = true,
                        Name = driveFolderName
                    };
                }
                else if (folders.Count > 1)
                {
                    throw new InvalidOperationException($"More than one folder named  {driveFolderName} was found.");
                }
            });

            if (folderCacheObj != null)
            {
                _driveObjectCache.Add(folderCacheObj);
            }

            return folderCacheObj;
        }

        private void ExecuteAuthenticated(Action<DriveService> action)
        {
            const int numberOfAttempts = 3;

            if (!IsAuthenticated)
            {
                throw new AuthenticationException("Please call InitiateAuthentication() first and wait for IsAuthenticated to become true or subscribe to Authenticated event to get a notification.");
            }

            for (int i = 0; i <= numberOfAttempts; i++)
            {
                try
                {
                    using (var service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = _credential,
                        ApplicationName = ProductConstants.PRODUCT_NAME
                    }))
                    {
                        action(service);
                        break;
                    }
                }
                catch (global::Google.Apis.Auth.OAuth2.Responses.TokenResponseException trex)
                {
                    if (trex.Message.Contains("invalid_grant"))
                    {
                        Reauthenticate();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (HttpRequestException hrex)
                {
                    if (!(hrex.InnerException != null &&
                        hrex.InnerException is WebException &&
                        hrex.InnerException.Message != null &&
                        hrex.InnerException.Message.Contains("oauth2.googleapis.com")))
                    {
                        throw;
                    }
                }
            }
        }

        private async Task Authenticate(bool updatePermissions = false)
        {
            string[] Scopes = { DriveService.Scope.DriveAppdata };

            try
            {
                if (updatePermissions)
                {
                    await _dataStore.ClearAsync();
                }

                using (var stream = this.GetType().Assembly
                        .GetManifestResourceStream(string.Format("{0}.Resources.Credentials.GoogleDriveCredentials.json",
                        this.GetType().Assembly.GetName().Name)))
                //using (var stream =
                //        new FileStream(@"CloudCredentials\GoogleDriveCredentials.json", FileMode.Open, FileAccess.Read))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException("Cannot find app credentials for Google Drive. The credentials json file must be an embedded resource in the current assembly.");
                    }
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        ProductConstants.PRODUCT_NAME, // username
                        CancellationToken.None,
                        _dataStore);
                }
            }
            catch (Exception ex)
            {
                AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.AuthenticationError, ex.Message, ex.ToString()));
            }
        }

        private void Reauthenticate()
        {
            GoogleWebAuthorizationBroker.ReauthorizeAsync(_credential, CancellationToken.None).Wait();
        }

        private class DriveObjectInfo
        {
            public string Name { get; set; }
            public string Id { get; set; }
            public bool IsFolder { get; set; }

            public override bool Equals(object obj)
            {
                return Id == (obj as DriveObjectInfo)?.Id;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }


        private class StringDataTransferEventArgs : EventArgs
        {
            public StringDataTransferEventArgs()
            { }

            public StringDataTransferEventArgs(string inData)
            {
                InData = inData;
            }

            public string InData { get; set; }
            public string OutData { get; set; }
        }

        private class EventDrivenDataStore : IDataStore
        {
            private static ConcurrentDictionary<string, string> _settings = new ConcurrentDictionary<string, string>();

            public event EventHandler<StringDataTransferEventArgs> DataModified;

            public event EventHandler<StringDataTransferEventArgs> FetchDataRequested;
            
            public Task ClearAsync()
            {
                _settings.Clear();
                return EventSyncSettingDictionary();
            }

            public Task DeleteAsync<T>(string key)
            {
                _settings.TryRemove(key, out _);
                return EventSyncSettingDictionary();
            }

            public Task<T> GetAsync<T>(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Key MUST have a value");
                }

                return Task<T>.Factory.StartNew(() =>
                {
                    StringDataTransferEventArgs args = new StringDataTransferEventArgs();
                    FetchDataRequested?.Invoke(this, args);
                    if (string.IsNullOrEmpty(args.OutData))
                    {
                        return default(T);
                    }

                    IDictionary<string, string> dic = null;
                    try
                    {
                        dic = NewtonsoftJsonSerializer.Instance.Deserialize<IDictionary<string, string>>(args.OutData);
                    }
                    catch 
                    {
                        return default(T);
                    }

                    foreach (string dicKey in dic.Keys)
                    {
                        _settings.AddOrUpdate(dicKey, dic[dicKey], (k, oldValue) => dic[dicKey]);
                    }

                    T value = default(T);
                    if (_settings.TryGetValue(key, out string val) && !string.IsNullOrEmpty(val))
                    {
                        value = NewtonsoftJsonSerializer.Instance.Deserialize<T>(val);
                    }

                    return value;
                });
            }

            public Task StoreAsync<T>(string key, T value)
            {
                string newVal = NewtonsoftJsonSerializer.Instance.Serialize(value);
                _settings.AddOrUpdate(key, newVal, (k, oldValue) => newVal);
                return EventSyncSettingDictionary();
            }

            private Task EventSyncSettingDictionary()
            {
                return Task.Run(() =>
                {
                    string json = NewtonsoftJsonSerializer.Instance.Serialize(_settings);
                    DataModified?.Invoke(this, new StringDataTransferEventArgs(json));
                });
            }
        }
    }
}
