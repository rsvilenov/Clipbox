using System;
using Clipbox.Services.Interfaces;
using Clipbox.Views;
using Clipbox.Win32;
using System.Windows;
using System.Windows.Threading;
using Clipbox.Views.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Settings;
using Clipbox.Infrastructure.UI.Windows.Enums;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Core.Extensibility.Contracts;
using System.Collections.Generic;

namespace Clipbox.Services
{
    public class MainClipboardViewWindowService : ClipboardViewWindowServiceBase, IMainClipboardViewWindowService
    {
        private ISettingsService _settingsService;
        
        private IMainWindow _mainWindow;

        private IClipboardManager _clipboardManager;

        private IPreviewGenerationService _previewGenerationService;

        private UIExtentionContainer _uiExtContainer;

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
                    var mainWindow = new MainWindow(_clipboardManager,
                        _previewGenerationService, 
                        _uiExtContainer, 
                        _settingsService, 
                        dm);
                    _mainWindow = mainWindow;

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

        private bool ShouldResizeToDpi()
        {
            return _settingsService.GetSetting<bool>(SettingsKeys.ResizeAccordingToMonitorOrSystemDpi);
        }

        public override IReadOnlyList<IFlowParameter> GetFlowParameters()
        {
            throw new NotImplementedException();
        }
    }
}
