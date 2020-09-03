namespace Clipbox.Extensions.BuiltIn.General
{
    public static class ExtensionSettingNames
    {
        public static string BlacklistedApps = nameof(BlacklistedApps);

        /// <summary>
        /// This is not actually a setting, but it is implemented as such in the extension,
        /// so that it can use the storage functionality of the SettingsService and its
        /// encryption capability, if the service is initialized with a encrypting IPersistenceProvider.
        /// </summary>
        public static string GoogleDriveAuthToken = nameof(GoogleDriveAuthToken);
    }
}
