using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipbox.ViewModels
{
    public class TrayContextMenuViewModel : BindableObject, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly IPauseService _pauseService;
        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;

        private bool _isDisposed;

        public event EventHandler ToggleMainWindowOpenRequested;
        public event EventHandler SettingsWindowOpenRequested;
        public event EventHandler QuitRequested;

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

        private bool _isToggleCloudSyncVisible;
        public bool IsToggleCloudSyncVisible
        {
            get { return _isToggleCloudSyncVisible; }
            set { SetField(ref _isToggleCloudSyncVisible, value); }
        }

        public TrayContextMenuViewModel(ISettingsService settingsService,
            IPauseService pauseService, 
            IMainClipboardViewWindowService mainClipboardViewWindowService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            _mainClipboardViewWindowService = mainClipboardViewWindowService ?? throw new ArgumentNullException(nameof(pauseService));

            _mainClipboardViewWindowService.MainWindowOpened += _mainClipboardViewWindowService_MainWindowOpened;
            _mainClipboardViewWindowService.MainWindowClosed += _mainClipboardViewWindowService_MainWindowClosed;

            IsToggleCloudSyncVisible = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudServicesEnabled);
            IsCloudSyncEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudSyncEnabled);
            IsPaused = !_settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled);

            ToggleCloudSyncEnabled = new DelegateCommand(ToggleCloudSyncEnabledHandler);
            Pause = new DelegateCommand<object>(PauseHandler);
            Resume = new DelegateCommand(ResumeHandler);
            ToggleMainWindowOpen = new DelegateCommand(() => Task.Run(() => ToggleMainWindowOpenRequested?.Invoke(this, new EventArgs())));
            OpenSettingsWindow = new DelegateCommand(() => Task.Run(() => SettingsWindowOpenRequested?.Invoke(this, new EventArgs())));
            Quit = new DelegateCommand(QuitHandler);

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _mainClipboardViewWindowService_MainWindowClosed(object sender, EventArgs e)
        {
            IsMainWindowOpen = false;
        }

        private void _mainClipboardViewWindowService_MainWindowOpened(object sender, EventArgs e)
        {
            IsMainWindowOpen = true;
        }

        private void _settingsService_SettingChanged(object sender, Services.EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.IsClipboardTrackingEnabled)
            {
                IsPaused = !_settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled);
            }
            else if (e.Key == SettingsKeys.IsCloudServicesEnabled)
            {
                IsToggleCloudSyncVisible = _settingsService.GetSetting<bool>(SettingsKeys.IsCloudServicesEnabled);
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
            QuitRequested?.Invoke(this, new EventArgs());
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

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _mainClipboardViewWindowService.MainWindowOpened -= _mainClipboardViewWindowService_MainWindowOpened;
                _mainClipboardViewWindowService.MainWindowClosed -= _mainClipboardViewWindowService_MainWindowClosed;
                _settingsService.SettingChanged -= _settingsService_SettingChanged;

                _isDisposed = true;
            }
        }
    }
}
