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
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class MainClipboardViewWindowService : ClipboardViewWindowServiceBase, IMainClipboardViewWindowService
    {
        private ISettingsService _settingsService;
        
        private IMainWindow _mainWindow;

        private IClipboardManager _clipboardManager;

        private IPreviewGenerationService _previewGenerationService;

        private UIExtentionContainer _uiExtContainer;

        public event EventHandler<SetToClipboardEventArgs> SetItemsToClipboardRequested;

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
            UIExtentionContainer uiExtContainer, 
            ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _previewGenerationService = previewGenerationService;
            _uiExtContainer = uiExtContainer;
            _settingsService = settingsService;
        }
        
        public void Close()
        {
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    _mainWindow.ClipbardItemsCombinabilityCheckRequested -= _mainWindow_ClipbardItemsCombinabilityCheckRequested;
                    _mainWindow.SetItemsToClipboardRequested -= _mainWindow_SetItemsToClipboardRequested;
                    _mainWindow.HousekeepingRequested -= _mainWindow_HousekeepingRequested;
                    _mainWindow.CloseRequested -= _mainWindow_CloseRequested;

                    _mainWindow.Close();
                    //_mainWindow.Close();
                    _mainWindow = null;
                    // Equivalent to Application.DoEvents() just in case...
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

#if DEBUG
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
#endif
                }));
        }

        public void OpenOnMonitor(MonitorUtils.DisplayMonitor dm)
        {
            MonitorUtils.DisplayMonitor currentMonitor = MonitorUtils.GetByCurrentMousePosition();
            bool resizeToDpi = ShouldResizeToDpi();

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
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
                        dm);
                    _mainWindow = mainWindow;
                    
                    _mainWindow.ClipbardItemsCombinabilityCheckRequested += _mainWindow_ClipbardItemsCombinabilityCheckRequested;
                    _mainWindow.SetItemsToClipboardRequested += _mainWindow_SetItemsToClipboardRequested;
                    _mainWindow.HousekeepingRequested += _mainWindow_HousekeepingRequested;
                    _mainWindow.CloseRequested += _mainWindow_CloseRequested;

                    mainWindow.ScaleMode = resizeToDpi ? DpiScaleMode.Custom : DpiScaleMode.None;
                    
                    mainWindow.SetMonitorDimensions(new MonitorDimensions
                    {
                        MonitorDPI = currentMonitor.Dpi,
                        WorkAreaHeight = currentMonitor.WorkingArea.Height,
                        WorkAreaWidth = currentMonitor.WorkingArea.Width
                    });
                    
                    _mainWindow.Show();
                }));
        }

        private void _mainWindow_CloseRequested(object sender, EventArgs e)
        {
            Close();
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
