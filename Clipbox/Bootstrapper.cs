using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.CloudSupport.CloudSupportProviders;
using Clipbox.Infrastructure.Configuration;
using Clipbox.Infrastructure.Diagnostics;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.FilesystemAccess;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.Search;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Infrastructure.UI.Localization.LozalizationProviders;
using Clipbox.Infrastructure.UI.Theme;
using Clipbox.Services;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Settings.Enums;
using Clipbox.ViewModels;
using Clipbox.Views;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace Clipbox
{
    /// <summary>
    /// Wire everything in our application. We will be doing poor man's DI, or pure DI, as it is now called.
    /// This class is our composition root, but it handles shutdown and error handling procedures as well (single responsibility does not matter at this level).
    /// </summary>
    public class Bootstrapper
    {
        FlowController _flowController;
        LowLevelKeyboardService _keyboardService;
        IClipboardManager _clipboardManager;
        ClipboardListenerService _clipboardListenerService;
        QuickClipboardViewWindowService _previewWindowService;
        FlowExtensionExecutorService _flowExtensionExecutorService;
        TrayIconService _trayService;
        ISettingsService _settingsService;
        CloudSynchronizationService _cloudSyncService;
        MainClipboardViewWindowService _mainClipboardViewWindowService;
        TrayContextMenuService _trayContextMenuService;
        PauseService _pauseService;
        TrayBandService _trayBandService;
        ExtensionLoader _extensionLoader;
        QuickViewClipboardContents _quickViewClipboardContents;
        PreviewGenerationService _previewGenerationService;
        SearchService _searchService;
        SettingsWindowService _settingsWindowService;
        LocalizationService _localizationService;
        IPersistenceProvider _persistenceProvider;
        RebuildIndexWindowService _rebuildIndexWindowService;
        ISearchProviderSelectorService _searchProviderSelectorService;
        IGuardCommunicationService _guardCommunicationService;

        private int _shutdownSequenceCallCount;

        public void PerformStartupSequence()
        {
            try
            {
                _persistenceProvider = new AppDataPersistenceProvider();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Persistence provider:{ex.Message}");
                Application.Current.Shutdown((int)ProcessExitCodes.Error);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                // ignore args.IsTerminating - we will "fail fast"
                ProcessUnhandledException(args.ExceptionObject);
            };

            ClearLastExceptionInfo();

            // some functions such as registry access cannot work properly
            // if the program is not compiled for the appropriate platform
            EnsurePlatformMatch();

            string[] startupArgs = Environment.GetCommandLineArgs();

            if (startupArgs.Any(arg => arg == "ShowBrushes"))
            {
                (new ThemePreview()).ShowDialog();
                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }
            else if (!startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_GUARD_ARG))
            {
                MessageBox.Show("This program is not meant to be started manually.");
                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }

            // Disable WER error handling.
            // The docs for WerRegisterRuntimeExceptionModule, WerReportSetUIOption, etc... are terribly scarce and obscure,
            // so we will disable WER and roll out our own error reporting mechanism.
            // We will disable it per user, not for all users, so that we won't need to write to HKLM.
            int status = Win32.Native.Wer.WerAddExcludedApplication(Process.GetCurrentProcess().ProcessName, false);
            if (status != 0)
            {
                MarshalExt.ThrowOnWin32Error();
            }

            ApplicationUtils.CacheWindowsAppInfo();


            IEncryptionProvider encryptionProvider = new AesEncryptionProvider(_persistenceProvider);

            // Initialize the settings service
            IConfigurationProvider configurationProvider = new EncryptedPersistableConfigurationProvider(_persistenceProvider, encryptionProvider);
            _settingsService = new SettingsService(configurationProvider);
            _settingsService.RegisterSettings(SettingsKeys.GetAllSettings());
            _settingsService.SettingChanged += _settingsService_SettingChanged;

            SetInitialSettings();

            // Load extension containers
            _extensionLoader = new ExtensionLoader();
            _extensionLoader.SetEnabledExtensions(GetEnabledExtensions());
            _extensionLoader.LoadContainers();


            IEnumerable<SettingsUIExtension> settingsExtensions = _extensionLoader.SettingsUIExtensionContainer.ResolveAll();
            foreach (ISettingsRegistrator ext in settingsExtensions)
            {
                _settingsService.RegisterSettings(ext.GetSettingKeys());
            }


            int sessionId = SessionUtils.GetCurrentSessionId();
            _guardCommunicationService = new GuardCommunicationService(_persistenceProvider, _settingsService, sessionId);


            string appGuid = CreateOrGetAppGuid();

            // Initialize the clipboard listener
            _clipboardListenerService = new ClipboardListenerService();

            // Initialize the clipboard manager
            _clipboardManager = new ClipboardManager(_clipboardListenerService, _extensionLoader.FormatsContainer, _extensionLoader.PersistenceSupportExtensionContainer, _persistenceProvider, encryptionProvider)
            {
                IsClipboardTrackingEnabled = _settingsService.GetSetting(SettingsKeys.IsClipboardTrackingEnabled)
            };

            _previewGenerationService = new PreviewGenerationService(_clipboardManager, _extensionLoader.PreviewGeneratorsContainer, _settingsService, _persistenceProvider);

            // Initialize the view model - the quick view should show as fast as possible, so we will create it beforehand
            var viewModel = new QuickViewkClipboardContentsViewModel(_clipboardManager, _previewGenerationService, _settingsService);

            // Prepare the preview window
            _quickViewClipboardContents = new QuickViewClipboardContents(viewModel, _extensionLoader.UIExtensionContainer, _settingsService);

            // Initialize the quick preview window service
            _previewWindowService = new QuickClipboardViewWindowService(_quickViewClipboardContents, _settingsService);

            // Initialize the FlowExtension executor service
            _flowExtensionExecutorService = new FlowExtensionExecutorService(_extensionLoader.FlowExtensionContainer, _settingsService);

            // Initialize the keyboard service
            _keyboardService = new LowLevelKeyboardService();

            _cloudSyncService = new CloudSynchronizationService(_clipboardManager, new GoogleDriveCloudSupportProvider(), appGuid);
            _cloudSyncService.IsEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);

            _pauseService = new PauseService(_settingsService);
            _trayContextMenuService = new TrayContextMenuService(_settingsService, _pauseService);

            _searchProviderSelectorService = new SearchProviderSelectorService(_settingsService, _persistenceProvider, encryptionProvider, sessionId);
            ISearchProvider searchProvider = _searchProviderSelectorService.GetSearchProvider();

            _searchService = new SearchService(_extensionLoader.SearchSupportExtensionContainer, _clipboardManager, searchProvider);

            // this view can show a little bit slower so we will not keep its instance alive the whole time - we will create it upon request within the service
            _mainClipboardViewWindowService = new MainClipboardViewWindowService(_clipboardManager,
                _previewGenerationService,
                _extensionLoader.UIExtensionContainer,
                _settingsService,
                _searchService,
                _cloudSyncService);


            // Initialize the flow controller
            _flowController = new FlowController(_clipboardManager, _keyboardService, _flowExtensionExecutorService, _previewWindowService, _mainClipboardViewWindowService);

            //_mainWindow = new MainWindow(_settingsService);
            //Application.Current.MainWindow = _mainWindow;

            _trayBandService = new TrayBandService(_mainClipboardViewWindowService, _trayContextMenuService, _settingsService);

            _trayService = new TrayIconService(_mainClipboardViewWindowService, _trayContextMenuService, _settingsService);

            _settingsWindowService = new SettingsWindowService(_settingsService, _mainClipboardViewWindowService, _trayContextMenuService, _extensionLoader.SettingsUIExtensionContainer);

            _rebuildIndexWindowService = new RebuildIndexWindowService(_searchService, _mainClipboardViewWindowService, _clipboardManager);

            // try to save processing time, but beware of the memory usage
            AsmAwareResxLocalizationProvider.Instance.KeepLiveResourceManagers = true;
            // the singleton instance will be our localization provider
            ILocalizationProvider localizationProvider = AsmAwareResxLocalizationProvider.Instance;
            // Set the translation provider to the TranslationSourcePool
            TranslationSourcePool.InitTranslationSourcePool(localizationProvider);
            // than pass it to the localization service. It must be the same instance,
            // so that changing the culture from within the service will affect the culture
            // of the TranslationSource.
            _localizationService = new LocalizationService(localizationProvider, _settingsService);

            if (!ThemeManager.IsLoaded)
            {
                ThemeManager.LoadThemeFromSystemTheme();
            }

            ThemeManager.TransparencySettingChanged += ThemeManager_TransparencySettingChanged;
        }

        private List<PersistableExtensionInfo> GetEnabledExtensions()
        {
            List<PersistableExtensionInfo> enabledExtensionInfo;
            if (!_settingsService.GetSetting<bool>(SettingsKeys.IsInitialConfigurationDone))
            {
#if RELEASE
                MessageBox.Show("Put the strong names!!!");
                List<string> buildInExtensionAssemblyFullNames = new List<string>
                {
                    // in the release version put the strong names here...
                    "Clipbox.Extensions.BuiltIn.General, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "Clipbox.Extensions.BuiltIn.HdropFormats, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "Clipbox.Extensions.BuiltIn.ImageFormats, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    "Clipbox.Extensions.BuiltIn.TextFormats, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
                };
#endif
                enabledExtensionInfo = new List<PersistableExtensionInfo>();

                foreach (PersistableExtensionInfo availableExtension  in  _extensionLoader
                    .GetAllAvailableExtensions()
                    .ToList())
                {
#if RELEASE
                    if (buildInExtensionAssemblyFullNames.Contains(availableExtension.AssemblyFullName))
#endif
                    {
                        enabledExtensionInfo.Add(availableExtension);
                    }
                }

                string xml = XmlSerialization.Serialize(enabledExtensionInfo);
                _settingsService.SetSetting(SettingsKeys.EnabledExtensions, xml);
                _settingsService.SetSetting(SettingsKeys.IsInitialConfigurationDone, true);
            }
            else
            {
                string xml = _settingsService.GetSetting<string>(SettingsKeys.EnabledExtensions);
                enabledExtensionInfo = XmlSerialization.Deserialize<List<PersistableExtensionInfo>>(xml);
            }

            return enabledExtensionInfo;
        }

        private void ClearLastExceptionInfo()
        {
            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY))
            {
                _persistenceProvider.Delete(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY);
            }

            if (_persistenceProvider.CheckIfKeyExists(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY))
            {
                _persistenceProvider.Delete(ProductConstants.MAIN_CRASHFILE_PERSISTENCE_KEY);
            }
        }

        private void SetInitialSettings()
        {
            _settingsService.SetSetting(SettingsKeys.PreviewItemHeightRatio, 0.13d);
            _settingsService.SetSetting(SettingsKeys.MaxPreviewItemsInGroupItem, 5);
            _settingsService.SetSetting(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi, true);
            _settingsService.SetSetting(SettingsKeys.DimNonFocusedItemsInQuickView, true);
            _settingsService.SetSetting(SettingsKeys.DimNonFocusedItemsInMainView, false);
            _settingsService.SetSetting(SettingsKeys.IsDeskBandEnabled, true);
            _settingsService.SetSetting(SettingsKeys.IsTrayIconEnabled, true);
            _settingsService.SetSetting(SettingsKeys.IsCloudSyncEnabled, false);
            _settingsService.SetSetting(SettingsKeys.MainClipboardViewBackgroundRenderMode, (int)MainClipboardViewBackgroundRenderMode.ImageBrush);
            _settingsService.SetSetting(SettingsKeys.IsBackgroundPreviewGenerationEnabled, true);
            _settingsService.SetSetting(SettingsKeys.AreFadeAnimationsEnabled, true);
            _settingsService.SetSetting(SettingsKeys.IsAutomaticCloudSyncOfPinnedItemsEnabled, true);
            _settingsService.SetSetting(SettingsKeys.MaxNonPinnedItemsInQuickView, 5);

            _settingsService.SetSetting(SettingsKeys.NumOfNotYetVisibleRowsToPreload, 0);

            // changing of these requires restrat for the change to take effect
            _settingsService.SetSetting(SettingsKeys.PreviewGenerationTimeout, 6000);
            _settingsService.SetSetting(SettingsKeys.PreviewCacheMaxSize, 10485760);
            _settingsService.SetSetting(SettingsKeys.MaxSimultaneousPreviewTasks, 5);

            // overrides MaxSimultaneousPreviewTasks
            _settingsService.SetSetting(SettingsKeys.AutoDetermineMaxSimultaneousPreviewTasks, false);
        }

        private void ThemeManager_TransparencySettingChanged(object sender, EventArgs e)
        {
            if (ThemeManager.IsAcryllicTransparencyEnabled && _settingsService.GetSetting<int>(SettingsKeys.MainClipboardViewBackgroundRenderMode) == (int)MainClipboardViewBackgroundRenderMode.Acryllic)
            {
                // TODO: set the previous value (store this value in another settings key)
                _settingsService.SetSetting(SettingsKeys.MainClipboardViewBackgroundRenderMode, (int)default(MainClipboardViewBackgroundRenderMode));
            }
        }

        private void _settingsService_SettingChanged(object sender, Services.EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.IsClipboardTrackingEnabled)
            {
                // create this file in AppData so that the deskband can find it. 
                // IMPORTANT: Do not use IPersistenceProvider here, because its implementation may change,
                // and we want to store the filename precisely in %APPDATA%\Clipbox, so that the
                // deskband Shell extension, which is written in c++, can read it.
                string disabledFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), "disabled");
                if ((bool)e.Value)
                {
                    File.Delete(disabledFileName);
                    _clipboardManager.IsClipboardTrackingEnabled = true;
                }
                else
                {
                    // disable the clipboard manager as well
                    _clipboardManager.IsClipboardTrackingEnabled = false;
                    File.Create(disabledFileName).Dispose();
                }
            }
            else if (e.Key == SettingsKeys.IsCloudSyncEnabled)
            {
                if (_cloudSyncService != null)
                {
                    _cloudSyncService.IsEnabled = (bool)e.Value;
                }
            }
        }

        private string CreateOrGetAppGuid()
        {
            const string APP_GUID_PERSISTENCE_KEY = "appGuid";
            
            string appGuid = null;

            if (_persistenceProvider.CheckIfKeyExists(APP_GUID_PERSISTENCE_KEY))
            {
                appGuid = _persistenceProvider.ReadText(APP_GUID_PERSISTENCE_KEY);
            }
            else
            {
                appGuid = Guid.NewGuid().ToString();
                _persistenceProvider.Write(APP_GUID_PERSISTENCE_KEY, appGuid);
            }

            return appGuid;
        }

        public void PerformShutdownSequence(bool isResultOfError)
        {
            if (Interlocked.Increment(ref _shutdownSequenceCallCount) == 1)
            {
                // try to dispose of everything, regardless of the exceptions that may occur
                TryDisposeObject(_pauseService);
                TryDisposeObject(_keyboardService);
                TryDisposeObject(_flowController);
                TryDisposeObject(_cloudSyncService);
                TryDisposeObject(_clipboardManager);
                TryDisposeObject(_flowExtensionExecutorService);
                TryDisposeObject(_previewWindowService);
                TryDisposeObject(_quickViewClipboardContents);
                TryDisposeObject(_previewGenerationService);
                TryDisposeObject(_clipboardListenerService);
                TryDisposeObject(_settingsWindowService);
                TryDisposeObject(_trayService);
                TryDisposeObject(_trayBandService);
                TryDisposeObject(_trayContextMenuService);
                TryDisposeObject(_rebuildIndexWindowService);
                TryDisposeObject(_searchService);
            }
        }

        private void TryDisposeObject(IDisposable obj)
        {
            if (obj != null)
            {
                try { obj.Dispose(); } catch { }
            }
        }

        private void ProcessUnhandledException(object exceptionObject)
        {
            Exception exception = (Exception)exceptionObject;

            try
            {
                DiagnosticsReport dr = DiagnosticsReportGenerator
                    .Generate(exception, includeExtensionInfo: true);
                string reportXml = XmlSerialization.Serialize(dr);

                _persistenceProvider.Write(ProductConstants.MAIN_PROCESS_FAILURE_XML_PERSISTENCE_KEY, reportXml);
            }
            catch (Exception ex)
            {
                // this will go to the stderr and will be captured by the guard
                throw new AggregateException(exception, ex);
            }
            
            try
            {
                PerformShutdownSequence(true);
            }
            catch { }

            Environment.Exit((int)ProcessExitCodes.Error);
        }

        private void EnsurePlatformMatch()
        {
            // Some functions like registry access will be restricted to
            // the WoW subsystem when we are running a 32 bit process on a 64
            // bit OS. So, just don't allow that.
            if (Environment.Is64BitOperatingSystem
                && IntPtr.Size == 4 /* int ptr size of 4 indicates that the program is compiled for 32 bit platform */)
            {
                throw new PlatformNotSupportedException("Please compile this program for 64 bit architecure.");
            }
        }
    }
}
