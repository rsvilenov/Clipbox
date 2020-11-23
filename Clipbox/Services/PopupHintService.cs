using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Services.Catalogues;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Clipbox.Services
{
    public class PopupHintService : IPopupHintService
    {
        private readonly ISettingsService _settingsService;
        private readonly object _lockHandle;
        private readonly SettingsStringCollection _seenToolTips;

        private PopupHint _currentPopupHint;
        private bool _isHintPopupOpen;

        public PopupHintService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _seenToolTips = new SettingsStringCollection();
            _lockHandle = new object();

            LoadSeenTtps();
            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.HintToopTipsSeenList)
            {
                lock (_lockHandle)
                {
                    _seenToolTips.Clear();
                    LoadSeenTtps();
                }
            }
        }

        private void LoadSeenTtps()
        {
            string seenTtpsXml = _settingsService.GetSetting(SettingsKeys.HintToopTipsSeenList);
            if (!string.IsNullOrEmpty(seenTtpsXml))
            {
                _seenToolTips.Load(seenTtpsXml);
            }
        }

        public void ShowHintPopup(string popupName, FrameworkElement anchorElement, bool isTopPointing)
        {
            if (_popupInfoDictionary.ContainsKey(popupName)
                && !_seenToolTips.IsInErrorState
                && !_seenToolTips.Contains(popupName))
            {
                anchorElement.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        try
                        {
                            string text = TranslationSourcePool.Instance
                                .GetSource()
                                .GetTranslation(_popupInfoDictionary[popupName]);

                            if (_isHintPopupOpen)
                            {
                                _currentPopupHint.IsOpen = false;
                                _isHintPopupOpen = false;
                            }

                            _isHintPopupOpen = true;
                            _currentPopupHint = new PopupHint
                            {
                                AnchorElement = anchorElement,
                                IsTopPointing = isTopPointing,
                                Text = text,
                                Tag = popupName,
                                IsOpen = true,
                                StaysOpen = true
                            };
                        }
                        catch
                        {
                            // TODO: ?
                            return;
                        }

                        _currentPopupHint.ClosedByUser += _currentPopupHint_ClosedByUser;
                        _currentPopupHint.IsVisibleChanged += _currentPopupHint_IsVisibleChanged;
                    }));
            }
        }

        public void HideHintPopup()
        {
            if (_currentPopupHint != null)
            {
                _currentPopupHint?.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        try
                        {
                            if (_currentPopupHint.IsOpen)
                            {
                                _currentPopupHint.IsOpen = false;
                            }
                        }
                        catch
                        {
                            // TODO: ?
                        }
                    }));
            }
        }

        private void _currentPopupHint_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                _currentPopupHint.ClosedByUser -= _currentPopupHint_ClosedByUser;
                _currentPopupHint.IsVisibleChanged -= _currentPopupHint_IsVisibleChanged;
            }
        }

        private void _currentPopupHint_ClosedByUser(object sender, EventArgs e)
        {
            string popupName = (string)_currentPopupHint.Tag;

            Task.Run(() =>
            {
                lock (_lockHandle)
                {
                    _seenToolTips.Add(popupName);

                    if (!_seenToolTips.IsInErrorState)
                    {
                        try
                        {
                            _settingsService.SetSetting(SettingsKeys.HintToopTipsSeenList, _seenToolTips.GetXml());
                        }
                        catch
                        {
                            // TODO: log
                        }
                    }
                }
            });
        }

        private void _currentPopupHint_ToolTipClosing(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            _currentPopupHint.ToolTipClosing += _currentPopupHint_ToolTipClosing;

            
        }

        private readonly Dictionary<string, string> _popupInfoDictionary = new Dictionary<string, string>()
        {
            { PopupHintNames.PP_HINT_CHARGE_CLIPBOARD, "LBL_HINT_CHARGE_CLIPBOARD" }
        };
    }
}
