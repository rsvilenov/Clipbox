using Clipbox.Core;
using Clipbox.Core.Diagnostics;
using Clipbox.Core.Enums;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using Clipbox.ExtendedClipboard;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Configuration;
using Clipbox.Infrastructure.Diagnostics;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Encryption.EncryptionProviders;
using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.Persistence;
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
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        AutoClearClipboardService _autoClearClipboardService;

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

            // Load extension containers
            _extensionLoader = new ExtensionLoader();
            PerformInitialConfiguration(_extensionLoader, _settingsService);
            _extensionLoader.SetEnabledExtensions(GetEnabledExtensions());
            _extensionLoader.LoadContainers();


            IEnumerable<SettingsUIExtension> settingsExtensions = _extensionLoader.SettingsUIExtensionContainer.ResolveAll();
            foreach (ISettingsRegistrator ext in settingsExtensions)
            {
                _settingsService.RegisterSettings(ext.GetSettingKeys());
            }



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



            int sessionId = SessionUtils.GetCurrentSessionId();
            _guardCommunicationService = new GuardCommunicationService(_persistenceProvider, _settingsService, sessionId);


            string appGuid = CreateOrGetAppGuid();

            // Initialize the clipboard listener
            _clipboardListenerService = new ClipboardListenerService();

            // Initialize the clipboard manager
            _clipboardManager = new ClipboardManager(_clipboardListenerService,
                _extensionLoader.FormatsContainer,
                _extensionLoader.PersistenceSupportExtensionContainer,
                _persistenceProvider,
                encryptionProvider,
                _settingsService.GetSetting(SettingsKeys.ClipboardFormatSupportExtTimeout),
                _settingsService.GetSetting(SettingsKeys.ClipboardListenningThrottleInterval),
                _settingsService.GetSetting(SettingsKeys.ClipboardOpenningTimeoutMs))
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

            _cloudSyncService = new CloudSynchronizationService(_clipboardManager, _settingsService, _extensionLoader.CloudSupportExtensionContainer, appGuid);
            
            _pauseService = new PauseService(_settingsService);
            
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

            
            _trayService = new TrayIconService(_mainClipboardViewWindowService, _trayContextMenuService, _settingsService, _pauseService);

            _settingsWindowService = new SettingsWindowService(_settingsService, _mainClipboardViewWindowService, _cloudSyncService, _extensionLoader.SettingsUIExtensionContainer);

            _trayContextMenuService = new TrayContextMenuService(_settingsService, _pauseService, _mainClipboardViewWindowService, _settingsWindowService);
            
            _trayBandService = new TrayBandService(_mainClipboardViewWindowService, _trayContextMenuService, _settingsService);

            _rebuildIndexWindowService = new RebuildIndexWindowService(_searchService, _mainClipboardViewWindowService, _clipboardManager);

            _autoClearClipboardService = new AutoClearClipboardService(_clipboardManager, _settingsService);

            ThemeManager.TransparencySettingChanged += ThemeManager_TransparencySettingChanged;
        }

        private List<PersistableExtensionInfo> GetEnabledExtensions()
        {
            string xml = _settingsService.GetSetting<string>(SettingsKeys.EnabledExtensions);
            return XmlSerialization.Deserialize<List<PersistableExtensionInfo>>(xml);
        }

        private void PerformInitialConfiguration(ExtensionLoader extensionLoader, ISettingsService settingsService)
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
            List<PersistableExtensionInfo> enabledExtensionInfo = new List<PersistableExtensionInfo>();
            foreach (PersistableExtensionInfo availableExtension in extensionLoader
                .GetAllAvailableExtensions()
                .Select(ei => ei.PersistableExtensionInfo)
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
            settingsService.SetSetting(SettingsKeys.EnabledExtensions, xml);

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProductConstants.DESKBAND_INSTALL_FAILURE_FLAG_FILENAME)))
            {
                settingsService.SetSetting(SettingsKeys.IsDeskBandEnabled, false);
                settingsService.SetSetting(SettingsKeys.IsTrayIconEnabled, true);
            }
            else
            {
                settingsService.SetSetting(SettingsKeys.IsDeskBandEnabled, true);
                settingsService.SetSetting(SettingsKeys.IsTrayIconEnabled, false);
            }

            // tell explorer that there is a new extension
            Shell32.SHChangeNotify(NativeEnums.SHCNE.SHCNE_ASSOCCHANGED, NativeEnums.SHChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            System.Threading.Thread.Sleep(1000);

            settingsService.SetSetting(SettingsKeys.IsInitialConfigurationDone, true);
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
                if ((bool)e.Value)
                {
                    _clipboardManager.IsClipboardTrackingEnabled = true;
                }
                else
                {
                    _clipboardManager.IsClipboardTrackingEnabled = false;
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
                TryDisposeObject(_autoClearClipboardService);
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
