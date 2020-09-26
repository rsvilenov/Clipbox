using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        public AutoClearClipboardService(IClipboardManager clipboardManager, ISettingsService settingsService)
        {
            _clipboardManager = clipboardManager;
            _settingsService = settingsService;
            _settingsService.SettingChanged += _settingsService_SettingChanged;
            
            _isAutoClearSysClipboardEnabled = _settingsService.GetSetting(SettingsKeys.IsAutoClearSysClipboardEnabled);
            _clearSysClipboardAfterSecondsOfInactivity = _settingsService.GetSetting(SettingsKeys.AutoClearSysClipboardAfterSeconds);
            
            _isAutoClearExtClipboardEnabled = _settingsService.GetSetting(SettingsKeys.IsAutoClearExtClipboardEnabled);
            _clearExtClipboardAfterSecondsOfInactivity = _settingsService.GetSetting(SettingsKeys.AutoClearExtClipboardAfterSeconds);

            _isAutoClearExtClipboardUponLogoffEnabled = _settingsService.GetSetting(SettingsKeys.IsAutoClearExtClipboardUponLogoffEnabled);
            _isAutoClearExtClipboardUponLockScreenEnabled = _settingsService.GetSetting(SettingsKeys.IsAutoClearExtClipboardUponLockScreenEnabled);

            _inactivityDetectionTimer = new Timer(INACTIVITY_POLLING_INTERVAL_MS);
            _inactivityDetectionTimer.AutoReset = true;
            _inactivityDetectionTimer.Elapsed += _inactivityDetectionTimer_Elapsed;

            Application.Current.SessionEnding += Current_SessionEnding;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.IsAutoClearSysClipboardEnabled)
            {
                _isAutoClearSysClipboardEnabled = (bool)e.Value;
            }
            else if (e.Key == SettingsKeys.AutoClearSysClipboardAfterSeconds)
            {
                _clearSysClipboardAfterSecondsOfInactivity = (int)e.Value;
            }
            else if (e.Key == SettingsKeys.IsAutoClearExtClipboardEnabled)
            {
                _isAutoClearExtClipboardEnabled = (bool)e.Value;
            }
            else if (e.Key == SettingsKeys.AutoClearExtClipboardAfterSeconds)
            {
                _clearExtClipboardAfterSecondsOfInactivity = (int)e.Value;
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
            if (_isAutoClearSysClipboardEnabled)
            {
                if (SessionUtils.GetLastInputTime() > _clearSysClipboardAfterSecondsOfInactivity * 1000)
                {
                    _clipboardManager.ClearSystemClipboard();
                }
            }

            if (_isAutoClearExtClipboardEnabled)
            {
                if (SessionUtils.GetLastInputTime() > _clearExtClipboardAfterSecondsOfInactivity * 1000)
                {
                    ClearStoredClipboardItemsForCurentSession();
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

            _clipboardManager.ClearSystemClipboard();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _inactivityDetectionTimer.Dispose();

                _settingsService.SettingChanged -= _settingsService_SettingChanged;
                Application.Current.SessionEnding -= Current_SessionEnding;
                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

                _isDisposed = true;
            }
        }
    }
}
