using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes.CloudSupportTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Infrastructure.Cache;
using Clipbox.Infrastructure.CloudSupport;
using Clipbox.Infrastructure.Network;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;

namespace Clipbox.Extensions.BuiltIn.General.CloudSupportExtensions
{
    [ClipboxExtension(
              description: "Microsoft OneDrive cloud support",
              author: ProductConstants.PRODUCT_NAME,
              version: 1.0,
              MinHostVersion = 1.0
              )]
    [CloudSupport("Microsoft OneDrive", "OneDrive.png")]
    public class OneDriveCloudSupportExtension : CloudSupportExtension, IAuthenticatable
    {
        // How to set up the app in Azure:
        // 1. Go to https://portal.azure.com
        // 2. Register the app for personal (skype, etc..) and organizatoin account (the third option)
        // 3. Go to Authentication -> Add a platform -> Mobile and desktop applications
        // 4. Chose the redirect Uri, save the configuration
        // 5. Copy the redirect URI and paste it below
        // 6. Copy the ClientId and Tenant and past them beow
        // 7. Go to Api Permissions and add Files.ReadWrite.AppFolder 
        // (Click "Add +", select Microsoft Graph, the biggest tile, and then Delegated Permissions -> Files...)

        const string clientId = "d9ae6177-7275-4d59-9b76-e55d5c1fa6fd";//"4cbc02a4-7728-4ad8-9056-ef1d688b59df"; //"76c0c5f1-001d-4224-8ef6-c4ac5ab52c0e";
        const string tenant = "9aa6d9b3-90fb-4471-ba5e-551c8521f146";//"9aa6d9b3-90fb-4471-ba5e-551c8521f146"; //"9aa6d9b3-90fb-4471-ba5e-551c8521f146";
        const string redirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";

        private readonly GraphServiceClient _graphServiceClient;

        private string _tokenCache;
        private CloudObjectCache _cloudObjectCache;
        public OneDriveCloudSupportExtension()
        {
            _cloudObjectCache = new CloudObjectCache();

            string[] scopes = new string[] { "Files.ReadWrite.AppFolder" };

            IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
                //.WithAuthority(new Uri($"https://login.microsoftonline.com/{tenant}"))
                .WithAuthority(new Uri($"https://login.microsoftonline.com/consumers/{tenant}"))
                .WithRedirectUri(redirectUri)
                .Build();

            app.UserTokenCache.SetBeforeAccess(TokenCacheBeforeAccess);
            app.UserTokenCache.SetAfterAccess(TokenCacheAfterAccess);

            _graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                async requestMessage =>
                {
                    var accounts = await app.GetAccountsAsync();
                    var firstAccount = accounts.FirstOrDefault();
                    AuthenticationResult authResult = null;
                    try
                    {
                        authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                            .ExecuteAsync();

                        if (!IsAuthenticated)
                        {
                            AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticated));
                            IsAuthenticated = true;
                        }
                    }
                    catch (MsalUiRequiredException)
                    {
                        try
                        {
                            IsAuthenticated = false;
                            AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticating));

                            authResult = await app.AcquireTokenInteractive(scopes)
                                .WithAccount(accounts.FirstOrDefault())
                                .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                                .ExecuteAsync();

                            AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.Authenticated));
                            IsAuthenticated = true;
                        }
                        catch (MsalException msalex)
                        {
                            AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.AuthenticationError, msalex.Message));
                        }
                    }
                    catch (Exception ex)
                    {
                        AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.AuthenticationError, ex.Message));
                        return;
                    }

                    if (authResult != null)
                    {
                        // Add token to outgoing requests
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                    }
                    else
                    {
                        AuthenticationStageChanged?.Invoke(this, new AuthenticationStageEventArgs(AuthenticationStage.AuthenticationError, "Unknown error (authResult is null)"));
                    }
                }));
        }

        private void TokenCacheAfterAccess(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                byte[] tokenCacheBytes = args.TokenCache.SerializeMsalV3();
                _tokenCache = System.Convert.ToBase64String(tokenCacheBytes);
            }
        }

        private void TokenCacheBeforeAccess(TokenCacheNotificationArgs args)
        {
            if (!string.IsNullOrEmpty(_tokenCache))
            {
                byte[] tokenCacheBytes = Convert.FromBase64String(_tokenCache);
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes);
            }
        }

        #region IAuthenticatable
        public bool IsAuthenticated { get; private set; }

        public event EventHandler<AuthenticationStageEventArgs> AuthenticationStageChanged;

        public void InitiateAuthentication(object loginInfo)
        {
            _graphServiceClient.Me.Drive.Special.AppRoot.Request().GetAsync();
        }

        #endregion
        public override bool CheckCategoryExists(string categoryName)
        {
            bool exists = false;

            if (_cloudObjectCache.CheckCategoryExists(categoryName))
            {
                return true;
            }

            ExecuteAuthenticated((graphService) =>
            {
                DriveItem categoryItem = _graphServiceClient.Me.Drive.Special.AppRoot.Children.Request().GetAsync().ConfigureAwait(false).GetAwaiter().GetResult()
                    .FirstOrDefault(di => di.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) && di.Folder != null);

                if (categoryItem != null)
                {
                    _cloudObjectCache.AddCategory(categoryName, categoryItem.Id);
                    exists = true;
                }
            });

            return exists;
        }

        public override bool CheckItemExists(string categoryName, string itemName)
        {
            bool exists = false;

            if (_cloudObjectCache.CheckItemExists(categoryName, itemName))
            {
                return true;
            }

            ExecuteAuthenticated((graphService) =>
            {
                DriveItem driveItem = ListDriveItems(categoryName, itemName)
                    .FirstOrDefault(di => di.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

                if (driveItem != null)
                {
                    exists = true;
                    _cloudObjectCache.AddItem(categoryName, itemName, driveItem.Id);
                }
            });

            return exists;
        }

        public override void CreateCategory(string categoryName)
        {
            var driveItem = new DriveItem
            {
                Name = categoryName,
                Folder = new Folder
                {
                },
                AdditionalData = new Dictionary<string, object>()
                {
                    {"@microsoft.graph.conflictBehavior", "rename"}
                }
            };

            ExecuteAuthenticated((graphService) =>
            {
                DriveItem newCategoryDriveItem = graphService.Me.Drive.Special.AppRoot.Children
                    .Request()
                    .AddAsync(driveItem)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                _cloudObjectCache.AddCategory(categoryName, newCategoryDriveItem.Id);
            });
        }

        public override void DeleteCategory(string categoryName)
        {
            ExecuteAuthenticated((graphService) =>
            {
                graphService.Me.Drive.Special.AppRoot.ItemWithPath(categoryName)
                    .Request()
                    .DeleteAsync()
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            });

            _cloudObjectCache.RemoveCategory(categoryName);
        }

        public override void DeleteItem(string categoryName, string itemName)
        {
            ExecuteAuthenticated((graphService) =>
            {
                graphService.Me.Drive.Special.AppRoot.ItemWithPath($"{categoryName}/{itemName}")
                    .Request()
                    .DeleteAsync()
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            });

            _cloudObjectCache.RemoveItem(categoryName, itemName);
        }

        public override byte[] GetItem(string categoryName, string itemName)
        {
            byte[] fileBytes = null;

            ExecuteAuthenticated((graphService) =>
            {
                using (Stream stream = graphService.Me.Drive.Special.AppRoot
                .ItemWithPath($"{categoryName}/{itemName}").Content.Request()
                .GetAsync().ConfigureAwait(false).GetAwaiter().GetResult())
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }
            });

            return fileBytes;
        }

        public override IEnumerable<string> ListItems(string categoryName, string itemNamePart = "")
        {
            return ListDriveItems(categoryName, itemNamePart).Select(di => di.Name);
        }

        private IEnumerable<DriveItem> ListDriveItems(string categoryName, string itemNamePart = "")
        {
            IEnumerable<DriveItem> itemList = null;

            ExecuteAuthenticated((graphService) =>
            {
                if (!string.IsNullOrEmpty(itemNamePart))
                {
                    itemList = graphService.Me.Drive.Special.AppRoot.ItemWithPath(categoryName)
                        .Search(itemNamePart).Request().GetAsync().ConfigureAwait(false).GetAwaiter().GetResult()
                        // the Graph's search method searches in the contents as well, so we have to filter the names only
                        .Where(c => c.Name.IndexOf(itemNamePart, StringComparison.OrdinalIgnoreCase) > -1);
                }
                else
                {
                    itemList = graphService.Me.Drive.Special.AppRoot.ItemWithPath(categoryName).Children
                        .Request().GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            });

            return itemList;
        }

        public override void PutItem(string categoryName, string itemName, byte[] data)
        {
            ExecuteAuthenticated((graphService) =>
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    DriveItem newDriveItem = graphService.Me.Drive.Special.AppRoot
                        .ItemWithPath($"{categoryName}/{itemName}").Content.Request()
                        .PutAsync<DriveItem>(ms).ConfigureAwait(false).GetAwaiter().GetResult();

                    _cloudObjectCache.AddItem(categoryName, itemName, newDriveItem.Id);
                }
            });
        }

        private void ExecuteAuthenticated(Action<GraphServiceClient> action)
        {
            const int numberOfAttempts = 3;

            if (!IsAuthenticated)
            {
                throw new System.Security.Authentication.AuthenticationException("Please call InitiateAuthentication() first and wait for IsAuthenticated to become true or subscribe to Authenticated event to get a notification.");
            }

            for (int i = 0; i <= numberOfAttempts; i++)
            {
                try
                {
                    action(_graphServiceClient);
                    break;
                }
                catch (ServiceException sex)
                {
                    if (sex.Error?.Code == "InvalidAuthenticationToken")
                    {
                        if (i <= numberOfAttempts)
                        {
                            continue;
                        }
                        else
                        {
                            throw new System.Security.Authentication.AuthenticationException($"Authentication failed after {numberOfAttempts} attempts.", sex);
                        }
                    }

                    if (sex.Error?.Code == "timeout")
                    {
                        throw new TimeoutException("Timeout", sex);
                    }

                    var innerException = sex.InnerException;
                    while (innerException != null)
                    {
                        if (innerException is SocketException)
                        {
                            throw innerException;
                        }

                        innerException = innerException.InnerException;
                    }

                    throw sex;
                }
            }
        }

        public override CloudServiceInfo GetCloudServiceInfo()
        {
            const string url = "http://onedrive.microsoft.com";
            bool isAvailable = false;
            string failureReason = string.Empty;

            try
            {
                isAvailable = NetworkUtils.CheckIsEndpointReachable(url, 80);
                if (!isAvailable)
                {
                    failureReason = "Unknown";
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
    }
}
