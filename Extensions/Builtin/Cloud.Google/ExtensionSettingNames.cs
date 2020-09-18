namespace Clipbox.Extensions.BuiltIn.Cloud.Google
{
    public static class ExtensionSettingNames
    {
        /// <summary>
        /// This is not actually a setting, but it is implemented as such in the extension,
        /// so that it can use the storage functionality of the SettingsService and its
        /// encryption capability, if the service is initialized with a encrypting IPersistenceProvider.
        /// </summary>
        public static string GoogleDriveAuthToken = nameof(GoogleDriveAuthToken);

        public static string GoogleDriveReauthRequested = nameof(GoogleDriveReauthRequested);
    }
}
