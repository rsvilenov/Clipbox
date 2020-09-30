using Clipbox.Core.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Clipbox.Settings
{
    public static class SettingsKeys
    {
        public static SettingKey<double> PreviewItemHeightRatio = new SettingKey<double>(nameof(PreviewItemHeightRatio), 0.13d);

        public static SettingKey<int> MaxPreviewItemsInGroupItem = new SettingKey<int>(nameof(MaxPreviewItemsInGroupItem), 5);

        public static SettingKey<bool> ResizeAccordingToMonitorOrSystemDpi = new SettingKey<bool>(nameof(ResizeAccordingToMonitorOrSystemDpi), true);

        public static SettingKey<bool> DimNonFocusedItemsInQuickView = new SettingKey<bool>(nameof(DimNonFocusedItemsInQuickView), false);

        public static SettingKey<bool> DimNonFocusedItemsInMainView = new SettingKey<bool>(nameof(DimNonFocusedItemsInMainView), false);

        public static SettingKey<bool> IsClipboardTrackingEnabled = new SettingKey<bool>(nameof(IsClipboardTrackingEnabled), true);

        public static SettingKey<bool> IsDeskBandEnabled = new SettingKey<bool>(nameof(IsDeskBandEnabled), true);

        public static SettingKey<bool> IsCloudServicesEnabled = new SettingKey<bool>(nameof(IsCloudServicesEnabled), false);

        public static SettingKey<bool> IsCloudSyncEnabled = new SettingKey<bool>(nameof(IsCloudSyncEnabled), false);
        
        public static SettingKey<int> CloudServiceAuthAttemptTimeout = new SettingKey<int>(nameof(CloudServiceAuthAttemptTimeout), 20000);

        public static SettingKey<int> MainClipboardViewBackgroundRenderMode = new SettingKey<int>(nameof(MainClipboardViewBackgroundRenderMode), (int)Enums.MainClipboardViewBackgroundRenderMode.ImageBrush);

        public static SettingKey<bool> IsTrayIconEnabled = new SettingKey<bool>(nameof(IsTrayIconEnabled), true);

        public static SettingKey<bool> IsBackgroundPreviewGenerationEnabled = new SettingKey<bool>(nameof(IsBackgroundPreviewGenerationEnabled), true);

        public static SettingKey<bool> AreFadeAnimationsEnabled = new SettingKey<bool>(nameof(AreFadeAnimationsEnabled), true);

        public static SettingKey<bool> IsAutomaticCloudSyncOfPinnedItemsEnabled = new SettingKey<bool>(nameof(IsAutomaticCloudSyncOfPinnedItemsEnabled), true);

        public static SettingKey<int> MaxNonPinnedItemsInQuickView = new SettingKey<int>(nameof(MaxNonPinnedItemsInQuickView), 5);

        public static SettingKey<int> MaxPinnedItems = new SettingKey<int>(nameof(MaxPinnedItems), 10);

        public static SettingKey<int> PreviewGenerationTimeout = new SettingKey<int>(nameof(PreviewGenerationTimeout), 6000);

        public static SettingKey<int> PreviewCacheMaxSize = new SettingKey<int>(nameof(PreviewCacheMaxSize), 10485760);

        public static SettingKey<int> MaxSimultaneousPreviewTasks = new SettingKey<int>(nameof(MaxSimultaneousPreviewTasks), 5);

        public static SettingKey<bool> AutoDetermineMaxSimultaneousPreviewTasks = new SettingKey<bool>(nameof(AutoDetermineMaxSimultaneousPreviewTasks), false);

        public static SettingKey<int> NumOfNotYetVisibleRowsToPreload = new SettingKey<int>(nameof(NumOfNotYetVisibleRowsToPreload), 3);

        public static SettingKey<int> MaxNumOfItemToLoadAtOnce = new SettingKey<int>(nameof(MaxNumOfItemToLoadAtOnce), 50);

        public static SettingKey<string> LocalizationCulture = new SettingKey<string>(nameof(LocalizationCulture), "en-US");

        public static SettingKey<int> OnItemStoringFlowExtensionsTimeoutMs = new SettingKey<int>(nameof(OnItemStoringFlowExtensionsTimeoutMs), 1000);

        public static SettingKey<int> OnItemStoringFlowExtensionsMaxTasks = new SettingKey<int>(nameof(OnItemStoringFlowExtensionsMaxTasks), 10);

        /// <summary>
        /// Restart is required. Remove this message when the implementation is complete.
        /// </summary>
        public static SettingKey<bool> UseSearchProviderInExternalProcess = new SettingKey<bool>(nameof(UseSearchProviderInExternalProcess), true);

        /// <summary>
        /// Restart is required. Remove this message when the implementation is complete.
        /// </summary>
        public static SettingKey<bool> RunMainProcessWithHighPriority = new SettingKey<bool>(nameof(RunMainProcessWithHighPriority), false);

        public static SettingKey<bool> IsSearchEnabled = new SettingKey<bool>(nameof(IsSearchEnabled), true);

        public static SettingKey<string> EnabledExtensions = new SettingKey<string>(nameof(EnabledExtensions), string.Empty);

        public static SettingKey<string> SelectedCloudSupportServiceName = new SettingKey<string>(nameof(SelectedCloudSupportServiceName), string.Empty);

        /// <summary>
        /// Negative value means that the setting is disabled
        /// </summary>
        public static SettingKey<int> AutoClearSysClipboardAfterSeconds = new SettingKey<int>(nameof(AutoClearSysClipboardAfterSeconds), -120);
        
        /// <summary>
        /// Negative value means that the setting is disabled
        /// </summary>
        public static SettingKey<int> AutoClearExtClipboardAfterSeconds = new SettingKey<int>(nameof(AutoClearExtClipboardAfterSeconds), -7200);

        public static SettingKey<bool> IsAutoClearExtClipboardUponLogoffEnabled = new SettingKey<bool>(nameof(IsAutoClearExtClipboardUponLogoffEnabled), false);

        public static SettingKey<bool> IsAutoClearExtClipboardUponLockScreenEnabled = new SettingKey<bool>(nameof(IsAutoClearExtClipboardUponLockScreenEnabled), false);


        public static SettingKey<int> ClipboardFormatSupportExtTimeout = new SettingKey<int>(nameof(ClipboardFormatSupportExtTimeout), 1000);
        
        public static SettingKey<int> ClipboardListenningThrottleInterval = new SettingKey<int>(nameof(ClipboardListenningThrottleInterval), 150);
        
        public static SettingKey<int> ClipboardOpenningTimeoutMs = new SettingKey<int>(nameof(ClipboardOpenningTimeoutMs), 500);


        /// <summary>
        /// This is a special key - it is false only upon the first start of the application. It should not be resettable by the user.
        /// </summary>
        public static SettingKey<bool> IsInitialConfigurationDone = new SettingKey<bool>(nameof(IsInitialConfigurationDone), false);

        internal static IEnumerable<ISettingKey> GetAllSettings()
        {
            return typeof(SettingsKeys)
               .GetFields(BindingFlags.Public | BindingFlags.Static)
               .Where(f => f.FieldType.GetInterface(nameof(ISettingKey)) != null)
               .Select(f => (ISettingKey)f.GetValue(null));
        }
    }
}
