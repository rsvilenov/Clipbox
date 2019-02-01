using Clipbox.Core.Enums;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.ViewModels
{
    public class TrayContextMenuViewModel : BindableObject
    {
        private ISettingsService _settingsService;
        
        public event EventHandler ToggleMainWindowOpenRequested;

        public event EventHandler OpenSettingsWindowRequested;

        public ICommand ToggleCloudSyncEnabled { get; }

        public ICommand TogglePaused { get; }

        public ICommand Quit { get; }

        public ICommand ToggleMainWindowOpen { get; }

        public ICommand OpenSettingsWindow { get; }

        private bool _isCloudSyncEnabled;
        public bool IsCloudSyncEnabled
        {
            get { return _isCloudSyncEnabled; }
            set { SetField(ref _isCloudSyncEnabled, value); }
        }

        private bool _isPaused;
        public bool IsPaused
        {
            get { return _isPaused; }
            set { SetField(ref _isPaused, value); }
        }

        private bool _isMainWindowOpen;
        public bool IsMainWindowOpen
        {
            get { return _isMainWindowOpen; }
            set { SetField(ref _isMainWindowOpen, value); }
        }

        public TrayContextMenuViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            IsCloudSyncEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);
            IsPaused = _settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled);

            ToggleCloudSyncEnabled = new DelegateCommand(ToggleCloudSyncEnabledHandler);
            TogglePaused = new DelegateCommand(TogglePausedHandler);
            ToggleMainWindowOpen = new DelegateCommand(ToggleMainWindowOpenHandler);

            Quit = new DelegateCommand(QuitHandler);
            OpenSettingsWindow = new DelegateCommand(() => OpenSettingsWindowRequested?.Invoke(this, new EventArgs()));
        }

        private void QuitHandler()
        {
            Application.Current.Shutdown((int)ProcessExitCodes.Ok);
        }

        private void ToggleMainWindowOpenHandler()
        {
            ToggleMainWindowOpenRequested?.Invoke(this, new EventArgs());
            IsMainWindowOpen = !IsMainWindowOpen;
        }

        private void TogglePausedHandler()
        {
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled))
            {
                _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, false);
                IsPaused = true;
            }
            else
            {
                _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, true);
                IsPaused = false;
            }
        }

        private void ToggleCloudSyncEnabledHandler()
        {
            if (_settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled))
            {
                _settingsService.SetSetting(SettingsKeys.IsCloudSyncEnabled, false);
                IsCloudSyncEnabled = false;
            }
            else
            {
                _settingsService.SetSetting(SettingsKeys.IsCloudSyncEnabled, true);
                IsCloudSyncEnabled = true;
            }
        }
    }
}
