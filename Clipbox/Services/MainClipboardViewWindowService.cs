using System;
using Clipbox.Services.Interfaces;
using Clipbox.Views;
using Clipbox.Win32;
using System.Windows;
using System.Windows.Threading;
using Clipbox.Views.Interfaces;
using Clipbox.ExtendedClipboard.Interfaces;

namespace Clipbox.Services
{
    public class MainClipboardViewWindowService : IMainClipboardViewWindowService
    {
        private ISettingsService _settingsService;
        
        private IMainWindow _mainWindow;

        private IClipboardManager _clipboardManager;

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

        public MainClipboardViewWindowService(IClipboardManager clipboardManager, ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
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
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    _mainWindow = new MainWindow(_clipboardManager, _settingsService, dm);
                    _mainWindow.Show();
                }));
        }
    }
}
