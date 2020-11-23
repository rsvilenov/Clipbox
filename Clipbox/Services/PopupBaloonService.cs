using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Clipbox.Services
{
    public class PopupBaloonService : IPopupBaloonService
    {
        private const int N_COPIED_ITEMS_TO_SHOW_QV_INFOBALOON_AFTER = 3;
        private const int DELAY_SHOW_UPDATE_NOTIFICATION_MS = 5 * 60 * 1000; // 5 min
        private const int BALOON_HEIGHT = 300;
        private const int BALOON_WIDTH = 550;

        private readonly IClipboardManager _clipboardManager;
        private readonly ISettingsService _settingsService;
        private readonly IUpdateNotificationService _updateNotificationService;

        private readonly SettingsStringCollection _seenToolTips;
        private readonly object _lockHandle;

        private int _numCopiedItemsSinceStartup;
        private PopupBaloon _baloon;
        private int _requestedShowPosX;
        private int _requestedShowPosY;
        private bool _isBaloonOpen;

        private const string BLN_QV_REMINDER_INITIAL = nameof(BLN_QV_REMINDER_INITIAL);
        private const string BLN_UPDATE_AVAILABLE = nameof(BLN_UPDATE_AVAILABLE);
        private readonly Dictionary<string, BaloonInfo> _baloonNameTranslationMapping = new Dictionary<string, BaloonInfo>
        {
            { 
                BLN_QV_REMINDER_INITIAL, new BaloonInfo
                {
                    TextTranslationKey = "LBL_BALOON_QV_REMINDER"
                }
            },
            {
                BLN_UPDATE_AVAILABLE, new BaloonInfo
                {
                    TextTranslationKey = "LBL_BALOON_UPDATE_AVAILABLE",
                    CustomActionTranslationKey ="BTN_BALOON_UPDATE_NOW",
                    CustomActionDismissTranslationKey = "BTN_BALOON_UPDATE_LATER"
                }
            }
        };

        public PopupBaloonService(IClipboardManager clipboardManager, 
            ISettingsService settingsService,
            IUpdateNotificationService updateNotificationService)
        {
            _settingsService = settingsService;
            _updateNotificationService = updateNotificationService;
            _updateNotificationService.UpdateAvailable += _updateNotificationService_UpdateAvailable;

            _lockHandle = new object();
            _seenToolTips = new SettingsStringCollection();
            LoadSeenTtps();
            
            _settingsService.SettingChanged += _settingsService_SettingChanged;

            _clipboardManager = clipboardManager;

            if (!CheckIsTooltipSeen(BLN_QV_REMINDER_INITIAL))
            {
                _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            }

            if (_updateNotificationService.IsUpdateAvailable)
            {
                ShowUpdateNotificationWithDelay();
            }
        }
        private void LoadSeenTtps()
        {
            string seenTtpsXml = _settingsService.GetSetting(SettingsKeys.BaloonToopTipsSeenList);
            if (!string.IsNullOrEmpty(seenTtpsXml))
            {
                _seenToolTips.Load(seenTtpsXml);
            }
        }

        private void ShowUpdateNotificationWithDelay()
        {
            Task.Delay(DELAY_SHOW_UPDATE_NOTIFICATION_MS)
                                .ContinueWith((t) =>
                                {
                                    if (!CheckIsTooltipSeen(BLN_UPDATE_AVAILABLE))
                                    {
                                        TryShowBaloon(BLN_UPDATE_AVAILABLE);
                                    }
                                });
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.BaloonToopTipsSeenList)
            {
                lock (_lockHandle)
                {
                    _seenToolTips.Clear();
                    LoadSeenTtps();
                }
            }
        }

        private void _updateNotificationService_UpdateAvailable(object sender, EventArgs e)
        {
            if (!CheckIsTooltipSeen(BLN_UPDATE_AVAILABLE))
            {
                TryShowBaloon(BLN_UPDATE_AVAILABLE);
            }
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ItemAddedEventArgs e)
        {
            if (++_numCopiedItemsSinceStartup >= N_COPIED_ITEMS_TO_SHOW_QV_INFOBALOON_AFTER
                && !CheckIsTooltipSeen(BLN_QV_REMINDER_INITIAL))
            {
                _clipboardManager.ItemAdded -= _clipboardManager_ItemAdded;
                TryShowBaloon(BLN_QV_REMINDER_INITIAL);
            }
        }

        private void CustomActionRequested(string baloonName)
        {
            if (baloonName == BLN_UPDATE_AVAILABLE)
            {
                _updateNotificationService.LaunchUpdate();
            }
        }

        private void ShowBaloon(string baloonName)
        {
            if (!_baloonNameTranslationMapping.ContainsKey(baloonName))
            {
                return;
            }

            BaloonInfo bi = _baloonNameTranslationMapping[baloonName];

            string text = TranslationSourcePool.Instance.GetSource().GetTranslation(bi.TextTranslationKey);
            string customActionText = null;
            if (!string.IsNullOrEmpty(bi.CustomActionTranslationKey))
            {
                customActionText = TranslationSourcePool.Instance.GetSource().GetTranslation(bi.CustomActionTranslationKey);
            }
            string customActionDismissText = null;
            if (!string.IsNullOrEmpty(bi.CustomActionDismissTranslationKey))
            {
                customActionDismissText = TranslationSourcePool.Instance.GetSource().GetTranslation(bi.CustomActionDismissTranslationKey);
            }

            TaskbarUtils.TaskbarInfo tbInfo = TaskbarUtils.GetTaskbarInfo();
            MonitorUtils.DisplayMonitor dm = MonitorUtils.GetPrimaryMonitor();
            if (dm.Bounds.Contains(tbInfo.Rectangle))
            {
                if (tbInfo.Rectangle.Width > tbInfo.Rectangle.Height)
                {
                    // the taskbar is horizontal
                    _requestedShowPosX = (dm.Bounds.Left + dm.Bounds.Width) - BALOON_WIDTH;
                    if (tbInfo.Rectangle.Top == dm.Bounds.Top)
                    {
                        // the taskbar is at the top
                        _requestedShowPosY = tbInfo.Rectangle.Top + tbInfo.Rectangle.Height;
                    }
                    else
                    {
                        // the taskbar is at the bottom
                        _requestedShowPosY = tbInfo.Rectangle.Top - BALOON_HEIGHT;
                    }
                }
                else
                {
                    // the taskbar is vertical
                    _requestedShowPosY = (dm.Bounds.Top + dm.Bounds.Height) - BALOON_HEIGHT;

                    if (tbInfo.Rectangle.Left == dm.Bounds.Left)
                    {
                        // the taskbar is at the left side
                        _requestedShowPosX = tbInfo.Rectangle.Left + tbInfo.Rectangle.Width;
                    }
                    else
                    {
                        // the taskbar is at the right side
                        _requestedShowPosX = dm.Bounds.Width - tbInfo.Rectangle.Width - BALOON_WIDTH;
                    }
                }
            }
            else
            {
                // if the taskbar is not on the primary monitor, don't show anything
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                {
                    if (_isBaloonOpen)
                    {
                        return;
                    }

                    _isBaloonOpen = true;
                    _baloon = new PopupBaloon(_settingsService)
                    {
                        Width = BALOON_WIDTH,
                        Height = BALOON_HEIGHT,
                        // these will be overriden by SetWindowPos in SourceInitialized,
                        // because setting them here causes unpredictable behaviour with high DPI settings
                        Top = _requestedShowPosY,
                        Left = _requestedShowPosX,
                        Text = text,
                        CustomActionButtonText = customActionText,
                        CustomActionDismissButtonText = customActionDismissText,
                        Tag = baloonName
                    };

                    _baloon.SourceInitialized += Baloon_SourceInitialized;
                    _baloon.UserConfirmed += _baloon_UserConfirmed;
                    _baloon.CustomActionRequested += _baloon_CustomActionRequested;
                    _baloon.Closing += _baloon_Closing;
                    _baloon.Show();
                }));
        }

        private void TryShowBaloon(string baloonName)
        {
            try
            {
                ShowBaloon(baloonName);
            }
            catch
            {
                // TODO: log
            }
        }

        private void _baloon_CustomActionRequested(object sender, EventArgs e)
        {
            string baloon = ((PopupBaloon)sender).Tag as string;
            if (baloon == null)
            {
                throw new InvalidOperationException("The baloon does not have a name (tag)");
            }

            Task.Run(() =>
            {
                CustomActionRequested(baloon);
            });
        }

        private void _baloon_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _baloon.SourceInitialized -= Baloon_SourceInitialized;
            _baloon.UserConfirmed -= _baloon_UserConfirmed;
            _baloon.CustomActionRequested -= _baloon_CustomActionRequested;
            _baloon.Closing -= _baloon_Closing;

            _isBaloonOpen = false;
        }

        private void _baloon_UserConfirmed(object sender, EventArgs e)
        {
            string baloonName = ((PopupBaloon)sender).Tag as string;
            if (baloonName == null)
            {
                throw new InvalidOperationException("The baloon does not have a name (tag)");
            }

            StoreIsTooltipSeen(baloonName);
        }

        private void Baloon_SourceInitialized(object sender, EventArgs e)
        {
            WindowUtils.SetWindowsPosition(_baloon.Handle,
                _requestedShowPosX,
                _requestedShowPosY,
                BALOON_WIDTH,
                BALOON_HEIGHT);
        }

        private bool CheckIsTooltipSeen(string toolTipName)
        {
            if (_seenToolTips.IsInErrorState)
            {
                // there was an error. do not show the tooltip again
                return true;
            }
            else
            {
                return _seenToolTips.Contains(toolTipName);
            }
        }

        private void StoreIsTooltipSeen(string toolTipName)
        {
            lock (_lockHandle)
            {
                _seenToolTips.Add(toolTipName);
                try
                {
                    _settingsService.SetSetting(SettingsKeys.BaloonToopTipsSeenList, _seenToolTips.GetXml());
                }
                catch
                {
                    // TODO: log
                }
            }
        }


        public struct BaloonInfo
        {
            public string TextTranslationKey { get; set; }
            public string CustomActionTranslationKey { get; set; }
            public string CustomActionDismissTranslationKey { get; set; }
        }

    }
}
