using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Serialization;
using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Clipbox.Services
{
    public class PopupBaloonService : IPopupBaloonService
    {
        private const int N_COPIED_ITEMS_TO_SHOW_QV_INFOBALOON_AFTER = 3;

        private readonly IClipboardManager _clipboardManager;
        private readonly ISettingsService _settingsService;
        private readonly IUpdateNotificationService _updateNotificationService;

        private int _numCopiedItemsSinceStartup;
        private PopupBaloon _baloon;
        private int _requestedShowPosX;
        private int _requestedShowPosY;
        private const int BALOON_HEIGHT = 300;
        private const int BALOON_WIDTH = 550;
        private List<string> _seenToolTips;

        private const string TTP_QV_REMINDER_INITIAL = nameof(TTP_QV_REMINDER_INITIAL);
        private readonly Dictionary<string, string> _ttpNameTranslationMapping = new Dictionary<string, string>
        {
            { TTP_QV_REMINDER_INITIAL, "LBL_BALOON_QV_REMINDER" }
        };

        public PopupBaloonService(IClipboardManager clipboardManager, 
            ISettingsService settingsService,
            IUpdateNotificationService updateNotificationService)
        {
            _settingsService = settingsService;
            _updateNotificationService = updateNotificationService;
            _updateNotificationService.UpdateAvailable += _updateNotificationService_UpdateAvailable;

            _settingsService.SettingChanged += _settingsService_SettingChanged;

            _clipboardManager = clipboardManager;

            _seenToolTips = GetSeenTooltips();
            if (!CheckIsTooltipSeen(TTP_QV_REMINDER_INITIAL))
            {
                _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;
            }
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.HelpToopTipsSeenList)
            {
                _seenToolTips = GetSeenTooltips();
            }
        }

        private void _updateNotificationService_UpdateAvailable(object sender, EventArgs e)
        {
            
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ItemAddedEventArgs e)
        {
            if (++_numCopiedItemsSinceStartup >= N_COPIED_ITEMS_TO_SHOW_QV_INFOBALOON_AFTER
                && !CheckIsTooltipSeen(TTP_QV_REMINDER_INITIAL))
            {
                _clipboardManager.ItemAdded -= _clipboardManager_ItemAdded;
                ShowBaloon(TTP_QV_REMINDER_INITIAL);
            }
        }

        private void ShowBaloon(string baloonName)
        {
            if (!_ttpNameTranslationMapping.ContainsKey(baloonName))
            {
                return;
            }

            string text = TranslationSourcePool.Instance.GetSource().GetTranslation(_ttpNameTranslationMapping[baloonName]);

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
                    _baloon = new PopupBaloon(_settingsService)
                    {
                        Width = BALOON_WIDTH,
                        Height = BALOON_HEIGHT,
                        // these will be overriden by SetWindowPos in SourceInitialized,
                        // because setting them here causes unpredictable behaviour with high DPI settings
                        Top = _requestedShowPosY,
                        Left = _requestedShowPosX,
                        Text = text,
                        Tag = baloonName
                    };

                    _baloon.SourceInitialized += Baloon_SourceInitialized;
                    _baloon.UserConfirmed += _baloon_UserConfirmed;
                    _baloon.Closing += _baloon_Closing;
                    _baloon.Show();
                }));
        }

        private void _baloon_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _baloon.SourceInitialized -= Baloon_SourceInitialized;
            _baloon.UserConfirmed -= _baloon_UserConfirmed;
            _baloon.Closing -= _baloon_Closing;
        }

        private void _baloon_UserConfirmed(object sender, EventArgs e)
        {
            string toolTipName = ((PopupBaloon)sender).Tag as string;
            if (toolTipName == null)
            {
                throw new InvalidOperationException("The tooltip does not have a name (tag)");
            }

            StoreIsTooltipSeen(toolTipName);
        }

        private void Baloon_SourceInitialized(object sender, EventArgs e)
        {
            WindowUtils.SetWindowsPosition(_baloon.Handle,
                _requestedShowPosX,
                _requestedShowPosY,
                BALOON_WIDTH,
                BALOON_HEIGHT);
        }

        private List<string> GetSeenTooltips()
        {
            string xml = _settingsService.GetSetting<string>(SettingsKeys.HelpToopTipsSeenList);
            if (string.IsNullOrEmpty(xml))
            {
                return new List<string>();
            }

            List<string> ttpList = null;

            try
            {
                ttpList = XmlSerialization.Deserialize<List<string>>(xml);
            }
            catch
            {
                // TODO: log
            }

            return ttpList;
        }

        private bool CheckIsTooltipSeen(string toolTipName)
        {
            if (_seenToolTips == null)
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
            if (_seenToolTips != null)
            {
                _seenToolTips.Add(toolTipName);
                try
                {
                    _settingsService.SetSetting(SettingsKeys.HelpToopTipsSeenList, XmlSerialization.Serialize(_seenToolTips));
                }
                catch
                {
                    // TODO: log
                }
            }
        }
    }
}
