using Clipbox.Services.Interfaces;
using Clipbox.ViewModels;
using Clipbox.Views;
using Clipbox.Views.Interfaces;
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
        private ISettingsService _settingsService;

        private ITrayContextMenu _trayContextMenu;

        private MouseLLHook _mouseHook;

        private TrayContextMenuViewModel _trayContextMenuViewModel;

        public bool IsContextMenuOpen { get; private set; }

        public event EventHandler SettingsWindowOpenRequested;

        public TrayContextMenuService(ISettingsService settingsService, IPauseService pauseService)
        {
            _settingsService = settingsService;

            _trayContextMenuViewModel = new TrayContextMenuViewModel(_settingsService, pauseService);
            _trayContextMenuViewModel.SettingsWindowOpenRequested += _trayContextMenuViewModel_SettingsWindowOpenRequested;

            _mouseHook = new MouseLLHook();
            _mouseHook.MouseAction += _mouseHook_MouseAction;

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
            _trayContextMenu = new TrayContextMenu(_trayContextMenuViewModel);
            _trayContextMenu.Opened += _trayContextMenu_Opened;

            _trayContextMenu.OpenAtPoint(p);
        }

        private void _trayContextMenuViewModel_SettingsWindowOpenRequested(object sender, EventArgs e)
        {
            SettingsWindowOpenRequested?.Invoke(this, e);
        }

        private void _trayContextMenu_Opened(object sender, EventArgs e)
        {
            _mouseHook.Start();
            IsContextMenuOpen = true;
        }

        public void Dispose()
        {
            _mouseHook.Dispose();
        }
    }
}
