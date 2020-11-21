using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Services.Interfaces;
using Clipbox.Views;
using Clipbox.Win32;
using System;
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
        public PopupBaloonService(IClipboardManager clipboardManager, 
            ISettingsService settingsService,
            IUpdateNotificationService updateNotificationService)
        {
            _settingsService = settingsService;
            _updateNotificationService = updateNotificationService;
            _updateNotificationService.UpdateAvailable += _updateNotificationService_UpdateAvailable;

            _clipboardManager = clipboardManager;
            _clipboardManager.ItemAdded += _clipboardManager_ItemAdded;

            ShowBaloonWithText("some test");
        }

        private void _updateNotificationService_UpdateAvailable(object sender, EventArgs e)
        {
            
        }

        private void _clipboardManager_ItemAdded(object sender, ExtendedClipboard.EventArgs.ItemAddedEventArgs e)
        {
            if (++_numCopiedItemsSinceStartup >= N_COPIED_ITEMS_TO_SHOW_QV_INFOBALOON_AFTER)
            {
                ShowQvInfoBaloon();
            }
        }

        private void ShowQvInfoBaloon()
        {

        }

        private void ShowBaloonWithText(string text)
        {
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
                        Text = text
                    };

                    _baloon.SourceInitialized += Baloon_SourceInitialized;
                    _baloon.Show();
                }));
        }

        private void Baloon_SourceInitialized(object sender, EventArgs e)
        {
            _baloon.SourceInitialized -= Baloon_SourceInitialized;

            WindowUtils.SetWindowsPosition(_baloon.Handle,
                _requestedShowPosX,
                _requestedShowPosY,
                BALOON_WIDTH,
                BALOON_HEIGHT);
        }
    }
}
