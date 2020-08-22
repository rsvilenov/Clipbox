using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using System;
using System.Timers;

namespace Clipbox.Services
{
    public class PauseService : IPauseService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private Timer _pauseTimer;

        public PauseService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _pauseTimer = new Timer();
            _pauseTimer.Elapsed += _pauseTimer_Elapsed;

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            // if someone else requests that the clipboard tracking should be re-enabled,
            // stop the pause timer and update the property IsClipboardTrackingPaused.
            if (e.Key == SettingsKeys.IsClipboardTrackingEnabled
                && _settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled)
                && IsClipboardTrackingPaused)
            {
                _pauseTimer.Stop();
                IsClipboardTrackingPaused = false;
            }
        }

        private void _pauseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, true);
            IsClipboardTrackingPaused = false;
        }

        public void PauseClipboardTrackingFor(long milliseconds)
        {
            if (IsClipboardTrackingPaused)
            {
                throw new InvalidOperationException("The clipboard tracking is alreay paused.");
            }

            IsClipboardTrackingPaused = true;
            _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, false);
            _pauseTimer.Interval = milliseconds;
            _pauseTimer.Start();
        }

        public void ResumeClipboardTracking()
        {
            if (IsClipboardTrackingPaused)
            {
                _pauseTimer.Stop();
                _settingsService.SetSetting(SettingsKeys.IsClipboardTrackingEnabled, true);
                IsClipboardTrackingPaused = false;
            }
        }

        /// <summary>
        /// True if the clipboard tracking is paused. If it is disabled
        /// this should be false, to distinguish between the two states.
        /// </summary>
        public bool IsClipboardTrackingPaused { get; private set; }

        /// <summary>
        /// The remaining milliseconds until the clipboard tracking is resumed.
        /// </summary>
        public double RemainingPauseMilliseconds
        {
            get
            {
                if (IsClipboardTrackingPaused)
                {
                    double remaining = _pauseTimer.Interval - DateTime.Now.Millisecond;

                    // if the timer is ellapsed in the meantime, return 0
                    return remaining >= 0 ? remaining : 0;
                }
                else
                {
                    return 0;
                }
            }
        }
        public void Dispose()
        {
            if (_pauseTimer != null)
            {
                ResumeClipboardTracking();

                _pauseTimer.Stop();
                _pauseTimer.Dispose();
                _pauseTimer = null;
            }

        }
    }
}
