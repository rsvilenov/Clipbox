using Clipbox.Core;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.Linq;
using System.Windows;

namespace Clipbox.Services
{

    public class TrayBandService : ITrayBandService, IDisposable
    {
        private const string CMD_LEFT_CLICK = "LC";
        private const string CMD_RIGHT_CLICK = "RC";

        private readonly Guid _deskbandGuid = new Guid("3C6D5961-E6C7-44F6-A522-A20DE33D2DAC");

        private const string CLIPBAND_NAME = "ClipBox"; //ProductConstants.PRODUCT_NAME;

        private NamedPipesServer _namedPipeServer;

        private TrayContextMenuHelper _trayContextMenuHelper;
        
        private ISettingsService _settingsService;

        private ITrayContextMenuService _trayContextMenuService;

        private IMainClipboardViewWindowService _mainClipboardViewWindowService;

        public TrayBandService(IMainClipboardViewWindowService mainClipboardViewWindowService,
            ITrayContextMenuService trayContextMenuService,
            ISettingsService settingsService)
        {
            _mainClipboardViewWindowService = mainClipboardViewWindowService;

            _settingsService = settingsService;
            _trayContextMenuService = trayContextMenuService;

            _trayContextMenuHelper = new TrayContextMenuHelper(_trayContextMenuService);

            int mySessionId = SessionUtils.GetCurrentSessionId();
            _namedPipeServer = new NamedPipesServer($"{ProductConstants.SHELL_EXTENTION_NAMED_PIPE_PREFIX}.{mySessionId}");
            _namedPipeServer.GotMessage += _namedPipeServer_GotMessage;
            _namedPipeServer.ErrorOccurred += _namedPipeServer_ErrorOccurred;

            if (_settingsService.GetSetting<bool>(SettingsKeys.IsDeskBandEnabled))
            {
                ShowDeskBand();
            }

            _settingsService.SettingChanged += _settingsService_SettingChanged;
        }

        private void _namedPipeServer_ErrorOccurred(object sender, NamedPipeErrorEventArgs e)
        {
            throw e.Exception;
        }

        private void ShowDeskBand()
        {
            if (!DeskbandUtils.IsTaskBandShown(_deskbandGuid))
            {
                DeskbandUtils.ShowDeskBand(_deskbandGuid, CLIPBAND_NAME, true);
            }
        }

        private void HideDeskBand()
        {
            if (DeskbandUtils.IsTaskBandShown(_deskbandGuid))
            {
                DeskbandUtils.HideDeskband(_deskbandGuid);
            }
        }

        private void _settingsService_SettingChanged(object sender, EventArgsClasses.SettingsEventArgs e)
        {
            if (e.SettingKey == SettingsKeys.IsDeskBandEnabled)
            {
                if ((bool)e.SettingValue)
                {
                    ShowDeskBand();
                }
                else
                {
                    HideDeskBand();
                }
            }
        }

        public void Dispose()
        {
            _namedPipeServer.Dispose();
            HideDeskBand();
        }

        private void ToggleMainWindowVisibility(IntPtr hWndDeskBand)
        {
            if (_mainClipboardViewWindowService.IsOpen)
            {
                _mainClipboardViewWindowService.Close();
            }
            else
            {
                _mainClipboardViewWindowService.OpenOnMonitor(MonitorUtils.GetByWindowHandle(hWndDeskBand));
            }
        }

        

        private void _namedPipeServer_GotMessage(object sender, NamedPipesResponseEventArgs e)
        {
            PipeMessage msg = PipeMessage.FromString(e.Response);
            switch (msg.Command)
            {
                case CMD_LEFT_CLICK:
                    ToggleMainWindowVisibility(msg.HWND);
                    break;
                case CMD_RIGHT_CLICK:
                    _trayContextMenuHelper.ToggleVisibility(msg.HWND);
                    break;
                default:
                    throw new InvalidProgramException($"Invalid pipe message: {e.Response}");
            }
        }

        /// <summary>
        /// Sometimes I get two WM_RBUTTONUP in a row, resulting from a single mouse click.
        /// It is much easier to handle this situation here, than in the DeskBand extention.
        /// </summary>
        private class TrayContextMenuHelper
        {
            private const int RBUTTONUP_MSG_DELAY_MS = 300;

            private ITrayContextMenuService _trayContextMenuService;
            
            private SlidingTimer _duplicatedRightClickSligingTimer;

            private IntPtr _lastDeskbandHwnd;

            public TrayContextMenuHelper(ITrayContextMenuService trayContextMenuService)
            {
                _trayContextMenuService = trayContextMenuService;

                _duplicatedRightClickSligingTimer = new SlidingTimer
                {
                    IntervalMs = RBUTTONUP_MSG_DELAY_MS
                };

                _duplicatedRightClickSligingTimer.Elapsed += _duplicatedRightClickSligingTimer_Elapsed1;
            }

            private void _duplicatedRightClickSligingTimer_Elapsed1(object sender, EventArgs e)
            {
                ToggleVisibilityInternal();
            }

            private void ToggleVisibilityInternal()
            {
                if (!_trayContextMenuService.IsContextMenuOpen)
                {
                    var deskbandRect = WindowUtils.GetWindowRectangle(_lastDeskbandHwnd);
                    var monitor = MonitorUtils.GetByWindowHandle(_lastDeskbandHwnd);
                    var scaleFactor = monitor.Dpi / 96;

                    _trayContextMenuService.ShowContextMenuAtPoint(new Point
                    {
                        X = deskbandRect.X / scaleFactor,
                        Y = deskbandRect.Y / scaleFactor
                    });
                }
            }

            public void ToggleVisibility(IntPtr deskbandHwnd)
            {
                _lastDeskbandHwnd = deskbandHwnd;
                _duplicatedRightClickSligingTimer.Slide();
            }
        }


        private class PipeMessage
        {
            public string Command { get; private set; }

            public IntPtr HWND { get; private set; }

            public static PipeMessage FromString(string msg)
            {
                string[] parts = msg.Split(' ');
                if (parts.Length != 2)
                {
                    throw new ArgumentException($"Malformatted message: {nameof(msg)}");
                }

                string[] hWndStr = parts[1].Split(':');
                if (hWndStr[0] != "hWnd" &&
                    hWndStr.Length != 2 &&
                    !hWndStr[1].All(c => char.IsDigit(c)))
                {
                    throw new ArgumentException($"Malformatted message - no hWnd information: {nameof(msg)}");
                }

                return new PipeMessage
                {
                    Command = parts[0],
                    HWND = new IntPtr(int.Parse(hWndStr[1]))
                };
            }
        }

    }
}
