using Clipbox.Core;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.Core.Extensibility.Exceptions;
using Clipbox.ExtendedClipboard;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.CloudSupport.CloudSupportProviders;
using Clipbox.Infrastructure.Configuration;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Persistence.FilesystemAccess;
using Clipbox.Infrastructure.Persistence.PersistenceProviders;
using Clipbox.Infrastructure.Search.SearchProviders;
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
        
        TrayBandService _trayBandService;

        ExtensionLoader _extensionLoader;

        QuickViewClipboardContents _quickViewClipboardContents;

        PreviewGenerationService _previewGenerationService;

        SearchService _searchService;

        SettingsWindowService _settingsWindowService;

        LocalizationService _localizationService;

        IPersistenceProvider _persistenceProvider;

        public void PerformStartupSequence()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                // ignore args.IsTerminating - we will "fail fast"
                ProcessUnhandledException(args.ExceptionObject);
            };
            
            string[] startupArgs = Environment.GetCommandLineArgs();

            if (startupArgs.Any(arg => arg == "ShowBrushes"))
            {
                (new ThemePreview()).ShowDialog();
                Application.Current.Shutdown((int)ProcessExitCodes.Ok);
                return;
            }

            if (!startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_SERVICE_ARG) &&
                !startupArgs.Any(arg => arg == ProductConstants.STARTED_FROM_GUARD_ARG))
            {
                DemandExternalStart();
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

            _persistenceProvider = new AppDataPersistenceProvider();

            string appGuid = CreateOrGetAppGuid();
            
            // Initialize the clipboard listener
            _clipboardListenerService = new ClipboardListenerService();

            // Load extension containers
            _extensionLoader = new ExtensionLoader();
            _extensionLoader.LoadContainers();
            _extensionLoader.StatelessExtensionResolved += (s, e) =>
            {
                // this may slow us down
                _persistenceProvider.Write("lastLoadedExtension.log", $"{e.ExtensionType.FullName} [{e.ExtensionType.Assembly.FullName}]");
            };

            // Initialize the clipboard manager
            _clipboardManager = new ClipboardManager(_clipboardListenerService, _extensionLoader.FormatsContainer, _extensionLoader.PersistenceSupportExtensionContainer, _persistenceProvider);

            // Initialize the settings service
            IConfigurationProvider configurationProvider = new PersistableConfigurationProvider(_persistenceProvider);
            _settingsService = new SettingsService(configurationProvider);
            _settingsService.RegisterSettings(SettingsKeys.GetAllSettings());
            _settingsService.SettingChanged += _settingsService_SettingChanged;

            IEnumerable<SettingsUIExtension> settingsExtensions = _extensionLoader.SettingsUIExtensionContainer.ResolveAll();
            foreach(ISettingsRegistrator ext in settingsExtensions)
            {
                _settingsService.RegisterSettings(ext.GetSettingKeys());
            }

            //_settingsService.SetSetting("Setting1", "set");

            //var newSet = _settingsService.GetSetting<string>("Setting1");

            _settingsService.SetSetting(SettingsKeys.PreviewItemHeightRatio, 0.13d);
            _settingsService.SetSetting(SettingsKeys.MaxPreviewItemsInGroupItem, 5);
            _settingsService.SetSetting(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi, true);
            _settingsService.SetSetting(SettingsKeys.DimNonFocusedItems, false);
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

            _previewGenerationService = new PreviewGenerationService(_clipboardManager, _extensionLoader.PreviewGeneratorsContainer, _settingsService, _persistenceProvider);
            
            // Initialize the view model - the quick view should show as fast as possible, so we will create it beforehand
            var viewModel = new QuickViewkClipboardContentsViewModel(_clipboardManager, _previewGenerationService, _settingsService);

            // Prepare the preview window
            _quickViewClipboardContents = new QuickViewClipboardContents(viewModel, _extensionLoader.UIExtensionContainer, _settingsService);

            // Initialize the quick preview window service
            _previewWindowService = new QuickClipboardViewWindowService(_quickViewClipboardContents, _settingsService);

            // Initialize the FlowExtension executor service
            _flowExtensionExecutorService = new FlowExtensionExecutorService(_extensionLoader.FlowExtensionContainer);

            // Initialize the keyboard service
            _keyboardService = new LowLevelKeyboardService();

            _cloudSyncService = new CloudSynchronizationService(_clipboardManager, new GoogleDriveCloudSupportProvider(), appGuid);
            _cloudSyncService.IsEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);

            _trayContextMenuService = new TrayContextMenuService(_settingsService);
            
            _searchService = new SearchService(_extensionLoader.SearchSupportExtensionContainer, _clipboardManager, new LuceneSearch());
            
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

            ILocalizationProvider localizationProvider = new ResxLozalizationProvider();
            // Set the translation provider to the TranslationSource
            TranslationSource.InitTranslationSource(localizationProvider);
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
                }
                else
                {
                    // disable the clipboard manager as well

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

        private void DemandExternalStart()
        {
            int mySessionId = SessionUtils.GetCurrentSessionId();

            if (ProcessUtils.CheckProcessExists(Path.GetFileNameWithoutExtension(ProductConstants.SERVICE_EXE_NAME)))
            {
                DemandStartFromService(mySessionId);
            }
            else if (ProcessUtils.CheckProcessExistsInSession(Path.GetFileNameWithoutExtension(ProductConstants.GUARD_EXE_NAME), mySessionId))
            {
                DemandStartFromGuard(mySessionId);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Neither the service {ProductConstants.SERVICE_EXE_NAME} is running, nor {ProductConstants.GUARD_EXE_NAME} is found in session {mySessionId}");
            }
        }

        private void DemandStartFromGuard(int mySessionId)
        {
            // send message to the elevated guard to start us elevated
            NamedPipesClient npClient = new NamedPipesClient($"{ProductConstants.SERVICE_GUARD_NAMED_PIPE_PREFIX}.{mySessionId}");

            // notify the guard of our existence, so that it can check whether an elevated process
            // by our name already exists and if not, start us elevated
            using (Process p = Process.GetCurrentProcess())
            {
                npClient.SendMessage(p.Id.ToString());
            }
        }

        private void DemandStartFromService(int mySessionId)
        {
            const int serviceSessionId = 0;
            // send message to the service to start us elevated
            NamedPipesClient npClient = new NamedPipesClient($"{ProductConstants.SERVICE_GUARD_NAMED_PIPE_PREFIX}.{serviceSessionId}");
            npClient.SendMessage(mySessionId.ToString());
        }

        public void PerformShutdownSequence(bool isResultOfError)
        {
            // try to dispose of everything, regardless of the exceptions that may occur
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
            TryDisposeObject(_searchService);
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
            FailureInfo failureInfo = FailureInfo.CreateWithSystemSnapshot();

            if (exceptionObject != null)
            {
                failureInfo.LoadExceptionObject(exceptionObject);
            }

            if (_extensionLoader != null && _extensionLoader.LoadedTypes != null)
            {
                failureInfo.LoadedExtensions = _extensionLoader.LoadedTypes.
                    Select(t => t.FullName).
                    ToList();
            }
            
            try
            {
                if (exceptionObject is ClipboxExtensionException)
                {
                    var extensionException = (ClipboxExtensionException)exceptionObject;

                    Type failedExtensionType = extensionException.ExtensionType;
                    if (failedExtensionType != null)
                    {
                        failureInfo.LoadFailedExtensionInfo(failedExtensionType);
                    }
                }
            }
            catch { }
            
            try
            {
                _persistenceProvider.Write("lastException.log", failureInfo.ToString());
                _persistenceProvider.Write("lastException.xml", failureInfo.ToXml());
            }
            catch { }
            
            try
            {
                PerformShutdownSequence(true);
            }
            catch { }

            Environment.Exit((int)ProcessExitCodes.Error);
        }
    }
}
