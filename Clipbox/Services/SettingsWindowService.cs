using Clipbox.Core.Extensibility.Enums;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Views.Settings;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    internal class SettingsWindowService : ISettingsWindowService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ICloudSynchronizationService _cloudSyncService;
        private readonly SettingsUIExtensionContainer _extContainer;

        IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private SettingsWindow _settingsWindow;

        public SettingsWindowService(ISettingsService settingsService,
            IMainClipboardViewWindowService mainClipboardViewWindowService,
            ICloudSynchronizationService cloudSyncService,
            SettingsUIExtensionContainer extContainer)
        {
            _settingsService = settingsService;
            _cloudSyncService = cloudSyncService;

            _mainClipboardViewWindowService = mainClipboardViewWindowService;
            _mainClipboardViewWindowService.OpenMainWindowRequested += _mainClipboardViewWindowService_OpenMainWindowRequested;
            _mainClipboardViewWindowService.SettingsWindowOpenRequested += _mainClipboardViewWindowService_SettingsWindowOpenRequested;

            _extContainer = extContainer;
        }

        private void _mainClipboardViewWindowService_SettingsWindowOpenRequested(object sender, SettingsWindowEventArgs e)
        {
            OpenSettingsWindow(e.Category);
        }

        private void _mainClipboardViewWindowService_OpenMainWindowRequested(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsSettingsWindowOpen)
            {
                BringSettingsWindowToFront();
                ShowMainViewRequestedPopup();
                e.Cancel = true;
            }
        }

        public bool IsSettingsWindowOpen { get { return _settingsWindow?.IsVisible == true; } }

        private void BringSettingsWindowToFront()
        {
            if (_settingsWindow?.IsVisible == true)
            {
                _settingsWindow?.ActivateFromTray();
            }
        }

        private void ShowMainViewRequestedPopup()
        {
            if (_settingsWindow?.IsVisible == true)
            {
                _settingsWindow?.ShowMainViewRequestedPopup();
            }
        }

        public void OpenSettingsWindow(SettingsCategory highlightCategory)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    if (IsSettingsWindowOpen)
                    {
                        BringSettingsWindowToFront();
                    }
                    else
                    {
                        _settingsWindow = new SettingsWindow(_settingsService, _cloudSyncService, _extContainer);
                        // this event never fires
                        //_settingsWindow.Closed += _settingsWindow_Closed;
                        _settingsWindow.Closing += _settingsWindow_Closing;
                        _settingsWindow.Show();
                    }

                    _settingsWindow.HighlightCategory(highlightCategory);
                }));
        }

        private void _settingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _settingsWindow.Closing -= _settingsWindow_Closing;
            _settingsWindow = null;
        }

        public void Dispose()
        {
            _mainClipboardViewWindowService.OpenMainWindowRequested -= _mainClipboardViewWindowService_OpenMainWindowRequested;
            _mainClipboardViewWindowService.SettingsWindowOpenRequested -= _mainClipboardViewWindowService_SettingsWindowOpenRequested;
        }
    }
}
