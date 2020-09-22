using Clipbox.Services.Interfaces;
using Clipbox.ViewModels;
using Clipbox.Views;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using Clipbox.Win32.LLHooks;
using Clipbox.Win32.Native;
using System;
using System.Windows;

namespace Clipbox.Services
{
    /// <summary>
    /// Manages the object lifetime and auto-close functionality of the tray context menu
    /// </summary>
    public class TrayContextMenuService : ITrayContextMenuService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private ITrayContextMenu _trayContextMenu;
        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private readonly ISettingsWindowService _settingsWindowService;
        private readonly MouseLLHook _mouseHook;
        private readonly TrayContextMenuViewModel _trayContextMenuViewModel;

        private bool _isDisposed;

        public bool IsContextMenuOpen { get; private set; }

        public TrayContextMenuService(ISettingsService settingsService,
            IPauseService pauseService,
            IMainClipboardViewWindowService mainClipboardViewWindowService,
            ISettingsWindowService settingsWindowService)
        {
            _settingsService = settingsService;
            _mainClipboardViewWindowService = mainClipboardViewWindowService;
            _settingsWindowService = settingsWindowService;

            _trayContextMenuViewModel = new TrayContextMenuViewModel(_settingsService, pauseService, mainClipboardViewWindowService);
            _trayContextMenuViewModel.SettingsWindowOpenRequested += _trayContextMenuViewModel_SettingsWindowOpenRequested;
            _trayContextMenuViewModel.ToggleMainWindowOpenRequested += _trayContextMenuViewModel_ToggleMainWindowOpenRequested;

            _mouseHook = new MouseLLHook();
            _mouseHook.MouseAction += _mouseHook_MouseAction;
        }

        private void _trayContextMenuViewModel_ToggleMainWindowOpenRequested(object sender, EventArgs e)
        {
            if (_mainClipboardViewWindowService.IsOpen)
            {
                _mainClipboardViewWindowService.Close();
            }
            else
            {
                MonitorUtils.DisplayMonitor dm = _trayContextMenu?.CurrentMonitor ?? MonitorUtils.GetByCurrentMousePosition();
                _mainClipboardViewWindowService.OpenOnMonitor(dm);
            }
        }

        private void _mouseHook_MouseAction(MouseEventArgs obj)
        {
            if (obj.MouseAction == NativeEnums.MouseMessages.WM_LBUTTONUP ||
                obj.MouseAction == NativeEnums.MouseMessages.WM_RBUTTONUP)
            {
                _trayContextMenu.Close();
                _mouseHook.Stop();

                _trayContextMenu.Opened -= _trayContextMenu_Opened;
                _trayContextMenu = null;
                
                IsContextMenuOpen = false;
            }
        }

        public void ShowContextMenuAtPoint(Point p)
        {
            if (_trayContextMenu == null)
            {
                _trayContextMenu = new TrayContextMenu(_trayContextMenuViewModel);
                _trayContextMenu.Opened += _trayContextMenu_Opened;
            }

            _trayContextMenu.OpenAtPoint(p);
        }

        private void _trayContextMenuViewModel_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            if (!_settingsWindowService.IsSettingsWindowOpen)
            {
                if (_mainClipboardViewWindowService.IsOpen)
                {
                    _mainClipboardViewWindowService.Close();
                }

                _settingsWindowService.OpenSettingsWindow();
            }
        }

        private void _trayContextMenu_Opened(object sender, EventArgs e)
        {
            _mouseHook.Start();
            IsContextMenuOpen = true;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_trayContextMenu != null)
                {
                    _trayContextMenu.Opened -= _trayContextMenu_Opened;

                    if (_trayContextMenu is IDisposable disposableContextMenu)
                    {
                        disposableContextMenu.Dispose();
                    }

                    if (_trayContextMenuViewModel is IDisposable disposableVm)
                    {
                        disposableVm.Dispose();
                    }
                }

                _mouseHook.Dispose();

                _isDisposed = true;
            }
        }
    }
}
