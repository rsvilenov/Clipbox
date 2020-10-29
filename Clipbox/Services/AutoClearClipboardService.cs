using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;

namespace Clipbox.Services
{
    public class AutoClearClipboardService : IAutoClearClipboardService, IDisposable
    {
        private const int INACTIVITY_POLLING_INTERVAL_MS = 1000; // 1s

        private readonly IClipboardManager _clipboardManager;
        private readonly ISettingsService _settingsService;
        private readonly Timer _inactivityDetectionTimer;

        private int _clearSysClipboardAfterSecondsOfInactivity;
        private bool _isAutoClearSysClipboardEnabled;

        private int _clearExtClipboardAfterSecondsOfInactivity;
        private bool _isAutoClearExtClipboardEnabled;

        private bool _isAutoClearExtClipboardUponLogoffEnabled;
        private bool _isAutoClearExtClipboardUponLockScreenEnabled;

        private bool _isDisposed;
        private bool _clearSysClipboardInactivityIntervalReached;
        private bool _clearExtClipboardInactivityIntervalReached;
        private Application _currentApplication;

        public AutoClearClipboardService(IClipboardManager clipboardManager, ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;
            _settingsService.SettingChanged += _settingsService_SettingChanged;
            
            _clearSysClipboardAfterSecondsOfInactivity = _settingsService.GetSetting(SettingsKeys.AutoClearSysClipboardAfterSeconds);
            _isAutoClearSysClipboardEnabled = _clearSysClipboardAfterSecondsOfInactivity > 0;

            _clearExtClipboardAfterSecondsOfInactivity = _settingsService.GetSetting(SettingsKeys.AutoClearExtClipboardAfterSeconds);
            _isAutoClearExtClipboardEnabled = _clearExtClipboardAfterSecondsOfInactivity > 0;

            _isAutoClearExtClipboardUponLogoffEnabled = _settingsService.GetSetting(SettingsKeys.IsAutoClearExtClipboardUponLogoffEnabled);
            _isAutoClearExtClipboardUponLockScreenEnabled = _settingsService.GetSetting(SettingsKeys.IsAutoClearExtClipboardUponLockScreenEnabled);

            _inactivityDetectionTimer = new Timer(INACTIVITY_POLLING_INTERVAL_MS);
            _inactivityDetectionTimer.AutoReset = true;
            _inactivityDetectionTimer.Elapsed += _inactivityDetectionTimer_Elapsed;
            _inactivityDetectionTimer.Start();

            // grab a reference to the current application object in order to
            // prevent null ref exception on dispose if the Application.Current has
            // alreday been set to null by WPF upon shutdown.
            _currentApplication = Application.Current;
            _currentApplication.SessionEnding += Current_SessionEnding;

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {if (e.Key == SettingsKeys.AutoClearSysClipboardAfterSeconds)
            {
                _clearSysClipboardAfterSecondsOfInactivity = (int)e.Value;
                _isAutoClearSysClipboardEnabled = _clearSysClipboardAfterSecondsOfInactivity > 0;
            }
            else if (e.Key == SettingsKeys.AutoClearExtClipboardAfterSeconds)
            {
                _clearExtClipboardAfterSecondsOfInactivity = (int)e.Value;
                _isAutoClearExtClipboardEnabled = _clearExtClipboardAfterSecondsOfInactivity > 0;
            }
            else if (e.Key == SettingsKeys.IsAutoClearExtClipboardUponLogoffEnabled)
            {
                _isAutoClearExtClipboardUponLogoffEnabled = (bool)e.Value;
            }
            else if (e.Key == SettingsKeys.IsAutoClearExtClipboardUponLockScreenEnabled)
            {
                _isAutoClearExtClipboardUponLockScreenEnabled = (bool)e.Value;
            }
        }

        private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (_isAutoClearExtClipboardUponLogoffEnabled)
            {
                ClearStoredClipboardItemsForCurentSession();
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // I am not sure whether the system waits for the execution of this event's subscribers
            // before logging off, so I will not handle Session LogOff here, but in the
            // SessionEnding event.

            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if (_isAutoClearExtClipboardUponLockScreenEnabled)
                {
                    ClearStoredClipboardItemsForCurentSession();
                }
            }
        }

        private void _inactivityDetectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double lastInputTimeMs = SessionUtils.GetLastInputTimeInMs();
            if (_isAutoClearSysClipboardEnabled)
            {
                if (lastInputTimeMs > _clearSysClipboardAfterSecondsOfInactivity * 1000
                    && !_clearSysClipboardInactivityIntervalReached)
                {
                    try { _clipboardManager.ClearSystemClipboard(); } catch { }
                    _clearSysClipboardInactivityIntervalReached = true;
                }
                else if (_clearSysClipboardInactivityIntervalReached && lastInputTimeMs < INACTIVITY_POLLING_INTERVAL_MS + 500)
                {
                    _clearSysClipboardInactivityIntervalReached = false;
                }
            }

            if (_isAutoClearExtClipboardEnabled)
            {
                if (lastInputTimeMs > _clearExtClipboardAfterSecondsOfInactivity * 1000
                    && !_clearExtClipboardInactivityIntervalReached)
                {
                    ClearStoredClipboardItemsForCurentSession();
                    _clearExtClipboardInactivityIntervalReached = true;
                }
                else if (_clearExtClipboardInactivityIntervalReached && lastInputTimeMs < INACTIVITY_POLLING_INTERVAL_MS + 500)
                {
                    _clearExtClipboardInactivityIntervalReached = false;
                }
            }
        }

        private void ClearStoredClipboardItemsForCurentSession()
        {
            List<string> idsForDeletion = _clipboardManager.GetAllItemIds((item) =>
                            !item.IsPinned
                            && !item.IsProtected
                            && !((ICloudSynchronizable)item).IsSynchronizedWithCloud
                            && !item.ShouldTransfer
                            && item.Source.SourceMachineInfo.MachineName == Environment.MachineName
                            && item.Source.SourceSessionInfo.SessionUniqueId == SessionUtils.GetSessionUniqueId())
                        .ToList();

            foreach (string id in idsForDeletion)
            {
                _clipboardManager.RemoveClipboardItem(id, beSilent: false, removeFromSystemClipboard: true);
            }

            try { _clipboardManager.ClearSystemClipboard(); } catch { }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _inactivityDetectionTimer.Dispose();

                _settingsService.SettingChanged -= _settingsService_SettingChanged;
                _currentApplication.SessionEnding -= Current_SessionEnding;
                _currentApplication = null;
                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

                _isDisposed = true;
            }
        }
    }
}
