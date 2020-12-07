using Clipbox.Infrastructure.UI.Localization;
using Clipbox.Services.Catalogues;
using Clipbox.Services.DataObjects;
using Clipbox.Services.Enums;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Views.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Clipbox.Services
{
    public class PopupHintService : IPopupHintService
    {
        private readonly ISettingsService _settingsService;
        private readonly object _lockHandle;
        private readonly SettingsStringCollection _seenToolTips;
        private readonly Dictionary<string, int> _shownCount;

        private PopupHint _currentPopupHint;
        private bool _isHintPopupOpen;

        public PopupHintService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _seenToolTips = new SettingsStringCollection();
            _lockHandle = new object();
            _shownCount = new Dictionary<string, int>();

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

        public bool HasPopupBeenSeen(string popupName)
        {
            if (!_popupInfoDictionary.ContainsKey(popupName))
            {
                throw new ArgumentException($"{popupName} is not registered with the service.");
            }

            return _seenToolTips.IsInErrorState
                || _seenToolTips.Contains(popupName);
        }

        public bool TryShowHintPopup(string popupName, FrameworkElement anchorElement, PopupHintArrowDirection arrowDirection, PopupHintPlacementRelativeToAnchor placement)
        {
            if (!_popupInfoDictionary.ContainsKey(popupName))
            {
                throw new ArgumentException($"{popupName} is not registered with the service.");
            }

            if (_settingsService.GetSetting(SettingsKeys.AreHintPopupsEnabled)
                && !_seenToolTips.IsInErrorState
                && !_seenToolTips.Contains(popupName))
            {
                try
                {
                    PopupHintDefinition phDef = _popupInfoDictionary[popupName];
                    string text = TranslationSourcePool.Instance
                        .GetSource()
                        .GetTranslation(phDef.MessageTranslationKey);

                    if (_isHintPopupOpen)
                    {
                        _currentPopupHint.IsOpen = false;
                        _isHintPopupOpen = false;
                    }

                    _isHintPopupOpen = true;
                    _currentPopupHint = new PopupHint
                    {
                        AnchorElement = anchorElement,
                        Text = text,
                        Tag = popupName,
                        StaysOpen = true,
                        IsQuestion = phDef.IsQuestion
                    };

                    switch (arrowDirection)
                    {
                        case PopupHintArrowDirection.Top:
                            _currentPopupHint.ArrowDirection = PopupArrowDirection.Top;
                            break;
                        case PopupHintArrowDirection.Left:
                            _currentPopupHint.ArrowDirection = PopupArrowDirection.Left;
                            break;
                        case PopupHintArrowDirection.None:
                            _currentPopupHint.ArrowDirection = PopupArrowDirection.None;
                            break;
                    }

                    switch (placement)
                    {
                        case PopupHintPlacementRelativeToAnchor.Right:
                            _currentPopupHint.PlacementRelativeToAnchor = PopupPlacementRelativeToAnchor.Right;
                            break;
                        case PopupHintPlacementRelativeToAnchor.Bottom:
                            _currentPopupHint.PlacementRelativeToAnchor = PopupPlacementRelativeToAnchor.Bottom;
                            break;
                    }

                    _currentPopupHint.Open();

                    if (!_shownCount.ContainsKey(popupName))
                    {
                        _shownCount.Add(popupName, 0);
                    }

                    _shownCount[popupName]++;
                }
                catch
                {
                    // TODO: ?
                    return false;
                }

                _currentPopupHint.ClosedByUser += _currentPopupHint_ClosedByUser;
                _currentPopupHint.Closed += _currentPopupHint_Closed;


                return true;
            }

            return false;
        }

        private void _currentPopupHint_Closed(object sender, EventArgs e)
        {
            if (_isHintPopupOpen)
            {
                DestroyCurrentPopupHint();
                _isHintPopupOpen = false;
            }


            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void DestroyCurrentPopupHint()
        {
            if (_currentPopupHint != null)
            {
                _currentPopupHint.ClosedByUser -= _currentPopupHint_ClosedByUser;
                _currentPopupHint.Closed -= _currentPopupHint_Closed;

                _currentPopupHint.AnchorElement = null;
                _currentPopupHint = null;
            }
        }

        public void HideHintPopup(bool setAsSeen = false)
        {
            if (_currentPopupHint != null)
            {
                try
                {
                    string popupName = (string)_currentPopupHint.Tag;
                    if (_currentPopupHint.IsOpen)
                    {
                        _currentPopupHint.Close();
                        _isHintPopupOpen = false;
                    }

                    DestroyCurrentPopupHint();

                    bool maxShowLimitReached = _shownCount.ContainsKey(popupName)
                        && _shownCount[popupName] >= _popupInfoDictionary[popupName].MaxShowTimes;

                    if (setAsSeen || maxShowLimitReached)
                    {
                        SetAsAlreadySeen(popupName);
                    }
                }
                catch
                {
                    // TODO: ?
                }
            }
        }

        private void _currentPopupHint_ClosedByUser(object sender, PopupHintResponseEventArgs e)
        {
            string popupName = (string)_currentPopupHint.Tag;
            PopupHintResponseEventArgs.PopupHintResponse response = e.Response;

            Task.Run(() =>
            {
                if (response != PopupHintResponseEventArgs.PopupHintResponse.Confirm
                && popupName == PopupHintNames.PP_HINT_CONTINUE_SHOWING_HINTS_QUESTION)
                {
                    _settingsService.SetSetting(SettingsKeys.AreHintPopupsEnabled, false);
                }

                SetAsAlreadySeen(popupName);
            });
        }

        private void SetAsAlreadySeen(string popupName)
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
        }

        private readonly Dictionary<string, PopupHintDefinition> _popupInfoDictionary = new Dictionary<string, PopupHintDefinition>()
        {
            {
                PopupHintNames.PP_HINT_CLOSE_MAIN_WINDOW,
                new PopupHintDefinition
                {
                    MessageTranslationKey = "LBL_HINT_CLOSE_MAIN_WINDOW",
                    IsQuestion = false,
                    MaxShowTimes = 1
                }
            },
            { 
                PopupHintNames.PP_HINT_CHARGE_CLIPBOARD,
                new PopupHintDefinition
                {
                    MessageTranslationKey = "LBL_HINT_CHARGE_CLIPBOARD",
                    IsQuestion = false,
                    MaxShowTimes = 2
                }
            },
            { 
                PopupHintNames.PP_HINT_UI_EXTENSIONS,
                new PopupHintDefinition
                {
                    MessageTranslationKey = "LBL_HINT_UI_EXTENSIONS",
                    IsQuestion = false,
                    MaxShowTimes = 2
                }
            },
            { 
                PopupHintNames.PP_HINT_SELECT_MULTIPLE,
                new PopupHintDefinition
                {
                    MessageTranslationKey = "LBL_HINT_SELECT_MULTIPLE",
                    IsQuestion = false,
                    MaxShowTimes = 2
                }
            },
            { 
                PopupHintNames.PP_HINT_FILTER_BAND,
                new PopupHintDefinition
                {
                    MessageTranslationKey = "LBL_HINT_FILTER_BAND",
                    IsQuestion = false,
                    MaxShowTimes = 2
                }
            },
            {
                PopupHintNames.PP_HINT_CONTINUE_SHOWING_HINTS_QUESTION,
                new PopupHintDefinition
                {
                    MessageTranslationKey = "LBL_HINT_CONTINUE_SHOWING_HINTS_QUESTION",
                    IsQuestion = true,
                    MaxShowTimes = 1
                }
            }
        };

        private class PopupHintDefinition
        {
            public string MessageTranslationKey { get; set; }
            public bool IsQuestion { get; set; }

            /// <summary>
            /// The maximum number of times the hint shows
            /// </summary>
            public int MaxShowTimes { get; set; }
        }
    }
}
