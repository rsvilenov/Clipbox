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
        private readonly ISettingsService _settingsService;
        private readonly IPauseService _pauseService;


        public event EventHandler ToggleMainWindowOpenRequested;

        public event EventHandler SettingsWindowOpenRequested;

        public ICommand ToggleCloudSyncEnabled { get; }

        public ICommand Pause { get; }

        public ICommand Resume { get; }

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

        public TrayContextMenuViewModel(ISettingsService settingsService, IPauseService pauseService)
        {
            _settingsService = settingsService;
            _pauseService = pauseService;

            IsCloudSyncEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);
            IsPaused = !_settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled);

            ToggleCloudSyncEnabled = new DelegateCommand(ToggleCloudSyncEnabledHandler);
            Pause = new DelegateCommand<object>(PauseHandler);
            Resume = new DelegateCommand(ResumeHandler);
            ToggleMainWindowOpen = new DelegateCommand(ToggleMainWindowOpenHandler);

            Quit = new DelegateCommand(QuitHandler);
            OpenSettingsWindow = new DelegateCommand(async () => SettingsWindowOpenRequested?.Invoke(this, new EventArgs()));

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, Services.EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.IsClipboardTrackingEnabled)
            {
                IsPaused = !_settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled);
            }
        }

        private void ResumeHandler()
        {
            if (_pauseService.IsClipboardTrackingPaused)
            {
                _pauseService.ResumeClipboardTracking();
            }
            else
            {
                _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, true);
            }

            IsPaused = false;
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

        private void PauseHandler(object intervalObj)
        {
            if (long.TryParse(intervalObj?.ToString(), out long interval))
            {
                if (interval == -1)
                {
                    _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, false);
                }
                else
                {
                    _pauseService.PauseClipboardTrackingFor(interval);
                }

                IsPaused = true;
            }
            else
            {
                throw new InvalidOperationException("Please provide a command parameter for the 'pause' options, which can be handled by long.Parse()");
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
