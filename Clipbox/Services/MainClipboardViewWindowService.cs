using Clipbox.Core.Extensibility.Contracts;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class MainClipboardViewWindowService : ClipboardViewWindowServiceBase, 
        IMainClipboardViewWindowService
    {
        private static readonly object _mainWindowLockHandle = new object();

        private readonly ISettingsService _settingsService;
        
        private readonly IClipboardManager _clipboardManager;

        private readonly IPreviewGenerationService _previewGenerationService;

        private readonly ISearchService _searchService;

        private readonly ICloudSynchronizationService _cloudSyncService;

        private readonly UIExtensionContainer _uiExtContainer;
        
        private IMainWindow _mainWindow;

        public event EventHandler<SetToClipboardEventArgs> SetItemsToClipboardRequested;

        public event EventHandler SettingsWindowOpenRequested;

        public event EventHandler RebuildIndexRequested;

        public event EventHandler<CancelEventArgs> OpenMainWindowRequested;

        public bool IsOpen
        {
            get
            {
                if (_mainWindow != null && _mainWindow.IsVisible)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public MainClipboardViewWindowService(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService, 
            UIExtensionContainer uiExtContainer, 
            ISettingsService settingsService,
            ISearchService searchService,
            ICloudSynchronizationService cloudSyncService)
        {
            _clipboardManager = clipboardManager;
            _previewGenerationService = previewGenerationService;
            _uiExtContainer = uiExtContainer;
            _settingsService = settingsService;
            _searchService = searchService;
            _cloudSyncService = cloudSyncService;
        }
        
        public void Close()
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    lock (_mainWindowLockHandle)
                    {
                        if (_mainWindow == null)
                        {
                            return;
                        }

                        _mainWindow.ClipbardItemsCombinabilityCheckRequested -= _mainWindow_ClipbardItemsCombinabilityCheckRequested;
                        _mainWindow.SetItemsToClipboardRequested -= _mainWindow_SetItemsToClipboardRequested;
                        _mainWindow.HousekeepingRequested -= _mainWindow_HousekeepingRequested;
                        _mainWindow.CloseRequested -= _mainWindow_CloseRequested;
                        _mainWindow.SettingsWindowOpenRequested -= _mainWindow_SettingsWindowOpenRequested;
                        _mainWindow.RebuildIndexRequested -= _mainWindow_RebuildIndexRequested;

                        _mainWindow.Close();
                        //_mainWindow.Close();
                        _mainWindow = null;
                    }
                }));
            
            // Equivalent to Application.DoEvents() just in case...
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif

        }

        public void OpenOnMonitor(MonitorUtils.DisplayMonitor dm)
        {
            // add the ability to cancel the openning of
            // the main window, so that for example 
            // another window can be open in its place.
            // If the indexes are rebuilding, we can show
            // the progress instead of the main window.
            var openEventArgs = new CancelEventArgs();
            OpenMainWindowRequested?.Invoke(this, openEventArgs);
            if (openEventArgs.Cancel)
            {
                return;
            }

            bool resizeToDpi = ShouldResizeToDpi();

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    lock (_mainWindowLockHandle)
                    {
                        // if by some reason we try to open the window twice,
                        // stop here...
                        if (_mainWindow != null)
                        {
                            return;
                        }

                        var mainWindow = new MainWindow(_clipboardManager,
                            _previewGenerationService,
                            _uiExtContainer,
                            _settingsService,
                            dm,
                            _searchService,
                            _cloudSyncService);
                        _mainWindow = mainWindow;

                        _mainWindow.ClipbardItemsCombinabilityCheckRequested += _mainWindow_ClipbardItemsCombinabilityCheckRequested;
                        _mainWindow.SetItemsToClipboardRequested += _mainWindow_SetItemsToClipboardRequested;
                        _mainWindow.HousekeepingRequested += _mainWindow_HousekeepingRequested;
                        _mainWindow.CloseRequested += _mainWindow_CloseRequested;
                        _mainWindow.SettingsWindowOpenRequested += _mainWindow_SettingsWindowOpenRequested;
                        _mainWindow.RebuildIndexRequested += _mainWindow_RebuildIndexRequested;

                        mainWindow.ScaleMode = resizeToDpi ? DpiScaleMode.Custom : DpiScaleMode.None;
                        

                        _mainWindow.Show();
                    }
                }));
        }

        private void _mainWindow_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            SettingsWindowOpenRequested?.Invoke(this, new EventArgs());
        }

        private void _mainWindow_RebuildIndexRequested(object sender, EventArgs e)
        {
            RebuildIndexRequested?.Invoke(this, new EventArgs());
        }

        private void _mainWindow_CloseRequested(object sender, EventArgs e)
        {
            // run a task so this handle can finish without waiting for the Close operation
            Task.Run(() => Close());
        }

        private void _mainWindow_HousekeepingRequested(object sender, HousekeepingEventArgs e)
        {
            base.RaiseHousekeepingRequestedEvent(e);
        }

        private void _mainWindow_SetItemsToClipboardRequested(object sender, SetToClipboardEventArgs e)
        {
            SetItemsToClipboardRequested?.Invoke(this, e);
        }

        private void _mainWindow_ClipbardItemsCombinabilityCheckRequested(object sender, EventArgsClasses.CombinabilityEventArgs e)
        {
            base.RaiseCombinabilityCheckRequestedEvent(e);
        }

        private bool ShouldResizeToDpi()
        {
            return _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);
        }

        public override IReadOnlyList<IFlowParameter> GetFlowParameters()
        {
            return _mainWindow.GetFlowParameters();
        }
    }
}
