using Clipbox.Services.Interfaces;
using Clipbox.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipbox.Services
{
    internal class SettingsWindowService : ISettingsWindowService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ITrayContextMenuService _trayContextMenuService;

        IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private SettingsWindow _settingsWindow;

        public SettingsWindowService(ISettingsService settingsService,
            IMainClipboardViewWindowService mainClipboardViewWindowService,
            ITrayContextMenuService trayContextMenuService)
        {
            _settingsService = settingsService;
            _trayContextMenuService = trayContextMenuService;

            _mainClipboardViewWindowService = mainClipboardViewWindowService;

            _mainClipboardViewWindowService.SettingsWindowOpenRequested += _mainClipboardViewWindowService_SettingsWindowOpenRequested;
            _trayContextMenuService.SettingsWindowOpenRequested += _trayContextMenuService_SettingsWindowOpenRequested;
        }

        private void _trayContextMenuService_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            OpenSettingsWindow();
        }

        private void _mainClipboardViewWindowService_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            OpenSettingsWindow();
        }

        private void OpenSettingsWindow()
        {
            _settingsWindow = new SettingsWindow(_settingsService);
            _settingsWindow.Show();
        }

        public void Dispose()
        {
            _mainClipboardViewWindowService.SettingsWindowOpenRequested -= _mainClipboardViewWindowService_SettingsWindowOpenRequested;
            _trayContextMenuService.SettingsWindowOpenRequested -= _trayContextMenuService_SettingsWindowOpenRequested;
        }
    }
}
