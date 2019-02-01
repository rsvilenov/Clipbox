using Clipbox.Core;
using Clipbox.Infrastructure.Persistence;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Clipbox.CloudServices
{
    /// <summary>
    /// Google Drive support.
    /// WARNING: This class is not thread safe.
    /// </summary>
    [Description("Google Drive storage")]
    public class GoogleDriveService : ICloudService, IAuthenticatable
    {
        public bool IsAuthenticated { get; private set; }


        public event EventHandler Authenticated;

        public event EventHandler<ErrorEventArgs> AuthenticationFailed;


        private UserCredential _credential;

        private List<DriveObjectInfo> _driveObjectCache;

        public GoogleDriveService()
        {
            _driveObjectCache = new List<DriveObjectInfo>();
        }
        
        public void CreateFolder(string remoteFolderName)
        {
            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = remoteFolderName,
                MimeType = "application/vnd.google-apps.folder"
            };
            
            ExecuteAuthenticated((service) =>
            {
                var request = service.Files.Create(folderMetadata);
                request.Fields = "id";

                var folder = request.Execute();

                _driveObjectCache.Add(new DriveObjectInfo
                {
                    Id = folder.Id,
                    Name = remoteFolderName,
                    IsFolder = true
                });
            });
        }

        public void DeleteFile(string remoteFolderName, string remoteFileName)
        {
            DriveObjectInfo fileCacheObj =  GetInfoForDriveFile(remoteFolderName, remoteFileName);
            if (fileCacheObj == null)
            {
                return;
            }

            ExecuteAuthenticated((service) =>
            {
                try { service.Files.Delete(fileCacheObj.Id).Execute(); }
                catch { } // TODO: ?

                if (_driveObjectCache.Contains(fileCacheObj))
                {
                    _driveObjectCache.Remove(fileCacheObj);
                }
            });
        }

        public bool CheckFolderExists(string driveFolderName)
        {
            return GetInfoForDriveFolder(driveFolderName) != null;
        }

        public bool CheckFileExists(string driveFolderName, string driveFileName)
        {
            // do not throw exceptions when the folder does not exists
            // (GetInfoForDriveFile throws when the folder is not found, so check the folder first)
            return GetInfoForDriveFolder(driveFolderName) != null &&
                GetInfoForDriveFile(driveFolderName, driveFileName) != null;
        }

        public void DeleteFolder(string remoteFolderName)
        {
            DriveObjectInfo folderCacheObj = GetInfoForDriveFolder(remoteFolderName);
            
            ExecuteAuthenticated((service) =>
            {
                service.Files.Delete(folderCacheObj.Id).Execute();

                if (_driveObjectCache.Contains(folderCacheObj))
                {
                    _driveObjectCache.Remove(folderCacheObj);
                }
            });
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
                listFolderRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{driveFolderName}' and trashed=false";
                
                IList<Google.Apis.Drive.v3.Data.File> folders = listFolderRequest.Execute()
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
                listFolderRequest.Q = $"'{folderCacheObj.Id}' in parents and trashed=false and name='{driveFileName}' ";

                IList<Google.Apis.Drive.v3.Data.File> files = listFolderRequest.Execute()
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

        public void DownloadFile(string remoteFolderName, string remoteFileName, Stream stream)
        {
            DriveObjectInfo fileCacheObj = GetInfoForDriveFile(remoteFolderName, remoteFileName);

            if (fileCacheObj == null)
            {
                throw new FileNotFoundException($"Remote file {remoteFileName} could not be found.");
            }

            ExecuteAuthenticated(service =>
            {
                var request = service.Files.Get(fileCacheObj.Id);
                request.Download(stream);
            });
        }

        public void InitiateAuthentication(object loginInfo)
        {
            Authenticate(false).ContinueWith(t =>
            {
                if (_credential != null)
                {
                    IsAuthenticated = true;
                    Authenticated?.Invoke(this, new EventArgs());
                }
            });
        }

        public void UploadFile(string remoteFolderName, string remoteFileName, Stream stream)
        {
            DriveObjectInfo folderCacheObj = GetInfoForDriveFolder(remoteFolderName);
            if (folderCacheObj == null)
            {
                throw new FileNotFoundException($"Remote folder {remoteFolderName} could not be found.");
            }

            string mimeType;
            if (Path.GetExtension(remoteFileName) == ".zip")
            {
                mimeType = "application/zip";
            }
            else if (Path.GetExtension(remoteFileName) == ".txt")
            {
                mimeType = "text/plain";
            }
            else
            {
                throw new NotImplementedException("Currently only zip and text files can be uploaded.");
            }

            if (Path.GetFileName(remoteFileName) != remoteFileName)
            {
                throw new InvalidOperationException($"Got path instead of filename: {remoteFileName}");
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = remoteFileName,
                Parents = new List<string>
                {
                    folderCacheObj.Id
                }
            };

            ExecuteAuthenticated(service =>
            {
                var uploadRequest = service.Files.Create(fileMetadata, stream, mimeType);
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
                    Name = remoteFileName
                });
            });
        }

        public IEnumerable<string> ListFiles(string remoteFolderName, string fileNamePart = "")
        {
            // There seems to be an undocumented (?) limit to the length of the "name/fullText contains" query parameter.
            if (fileNamePart?.Length > 30)
            {
                throw new ArgumentOutOfRangeException($"{nameof(fileNamePart)} cannot be more than 30 characters long.");
            }

            DriveObjectInfo folderCacheObj = GetInfoForDriveFolder(remoteFolderName);
            if (folderCacheObj == null)
            {
                throw new FileNotFoundException($"Remote folder {remoteFolderName} could not be found.");
            }

            // TODO: make this use the paging functionality of google drive
            // https://stackoverflow.com/questions/41572228/how-to-list-of-more-than-1000-records-from-google-drive-api-v3-in-c-sharp
            IList<Google.Apis.Drive.v3.Data.File> files = null;
            ExecuteAuthenticated(service =>
            {
                FilesResource.ListRequest listFolderRequest = service.Files.List();
                listFolderRequest.Fields = "files(id, name)";
                listFolderRequest.Q = $"'{folderCacheObj.Id}' in parents and trashed=false ";
                if (fileNamePart != string.Empty)
                {
                    listFolderRequest.Q += $" and name contains '{fileNamePart}'";
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
                        ApplicationName = ProductConstants.PRODUCT_NAME,
                    }))
                    {
                        action(service);
                        break;
                    }
                }
                catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException trex)
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
            string[] Scopes = { DriveService.Scope.DriveFile };

            try
            {
                string credPath = Path.Combine(AppDataUtils.GetAppDataDir(false), "token.json");

                if (updatePermissions)
                {
                    await new FileDataStore(credPath, true).ClearAsync();
                }

                using (var stream =
                        new FileStream(@"CloudCredentials\GoogleDriveCredentials.json", FileMode.Open, FileAccess.Read))
                {
                    // The file token.json stores the user's access and refresh tokens, and is created
                    // automatically when the authorization flow completes for the first time.
                    _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        ProductConstants.PRODUCT_NAME, // username
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }
            }
            catch (Exception ex)
            {
                AuthenticationFailed?.Invoke(this, new ErrorEventArgs(ex));
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
    }
}
