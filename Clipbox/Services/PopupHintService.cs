using Clipbox.Services.DataObjects;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Clipbox.Services
{
    public class PopupHintService : IPopupHintService
    {
        private readonly ISettingsService _settingsService;

        private SettingsStringCollection _seenToolTips;
        private PopupHint _currentPopupHint;
        private readonly object _lockHandle;

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
            lock (_lockHandle)
            {
                _seenToolTips.Clear();
                LoadSeenTtps();
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
                _currentPopupHint = new PopupHint
                {
                    AnchorElement = anchorElement,
                    IsTopPointing = isTopPointing,
                    Text = _popupInfoDictionary[popupName],
                    Tag = popupName
                };

                _currentPopupHint.Closed += _currentPopupHint_Closed;
            }
        }

        private void _currentPopupHint_Closed(object sender, EventArgs e)
        {
            _currentPopupHint.Closed -= _currentPopupHint_Closed;

            lock (_lockHandle)
            {
                _seenToolTips.Add((string)_currentPopupHint.Tag);

                if (!_seenToolTips.IsInErrorState)
                {
                    _settingsService.SetSetting(SettingsKeys.HintToopTipsSeenList, _seenToolTips.GetXml());
                }
            }
        }

        private readonly Dictionary<string, string> _popupInfoDictionary = new Dictionary<string, string>()
        {
            { PopupNames.PP_HINT_CHARGE_CLIPBOARD, "TTP_HINT_CHARGE_CLIPBOARD" }
        };

        public static class PopupNames
        {
            public const string PP_HINT_CHARGE_CLIPBOARD = nameof(PP_HINT_CHARGE_CLIPBOARD);
        }
    }
}
