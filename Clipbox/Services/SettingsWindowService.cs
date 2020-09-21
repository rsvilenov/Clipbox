using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Services.Interfaces;
using Clipbox.Views.Settings;
using System;
using System.Windows.Threading;

namespace Clipbox.Services
{
    internal class SettingsWindowService : ISettingsWindowService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ITrayContextMenuService _trayContextMenuService;
        private readonly ICloudSynchronizationService _cloudSyncService;
        private readonly SettingsUIExtensionContainer _extContainer;

        IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private SettingsWindow _settingsWindow;

        public SettingsWindowService(ISettingsService settingsService,
            IMainClipboardViewWindowService mainClipboardViewWindowService,
            ITrayContextMenuService trayContextMenuService,
            ICloudSynchronizationService cloudSyncService,
            SettingsUIExtensionContainer extContainer)
        {
            _settingsService = settingsService;
            _trayContextMenuService = trayContextMenuService;
            _cloudSyncService = cloudSyncService;

            _mainClipboardViewWindowService = mainClipboardViewWindowService;

            _extContainer = extContainer;

            _mainClipboardViewWindowService.SettingsWindowOpenRequested += _mainClipboardViewWindowService_SettingsWindowOpenRequested;
            _mainClipboardViewWindowService.OpenMainWindowRequested += _mainClipboardViewWindowService_OpenMainWindowRequested;
            _trayContextMenuService.SettingsWindowOpenRequested += _trayContextMenuService_SettingsWindowOpenRequested;
        }

        private void _mainClipboardViewWindowService_OpenMainWindowRequested(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_settingsWindow?.IsVisible == true)
            {
                _settingsWindow?.ActivateFromTray();
                e.Cancel = true;
            }
        }

        private void _trayContextMenuService_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            _mainClipboardViewWindowService.Close();
            OpenSettingsWindow();
        }

        private void _mainClipboardViewWindowService_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            OpenSettingsWindow();
        }

        private void OpenSettingsWindow()
        {
            // TODO: memory leak - the window never frees its memory
            _settingsWindow = new SettingsWindow(_settingsService, _cloudSyncService, _extContainer);
            _settingsWindow.Show();
        }

        public void Dispose()
        {
            _mainClipboardViewWindowService.SettingsWindowOpenRequested -= _mainClipboardViewWindowService_SettingsWindowOpenRequested;
            _trayContextMenuService.SettingsWindowOpenRequested -= _trayContextMenuService_SettingsWindowOpenRequested;
        }
    }
}
