using Clipbox.Core;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Clipbox.Services
{

    public class TrayBandService : ITrayBandService, IDisposable
    {
        private const string CMD_LEFT_CLICK = "LC";
        private const string CMD_RIGHT_CLICK = "RC";
        private const string CLIPBAND_NAME = "ClipBox"; //ProductConstants.PRODUCT_NAME;

        private readonly Guid _deskbandGuid = new Guid("3C6D5961-E6C7-44F6-A522-A20DE33D2DAC");

        private readonly NamedPipesServer _namedPipeServer;
        private readonly TrayContextMenuHelper _trayContextMenuHelper;

        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private readonly ITrayContextMenuService _trayContextMenuService;
        private readonly ISettingsService _settingsService;

        public TrayBandService(IMainClipboardViewWindowService mainClipboardViewWindowService,
            ITrayContextMenuService trayContextMenuService,
            ISettingsService settingsService)
        {
            _mainClipboardViewWindowService = mainClipboardViewWindowService;

            _settingsService = settingsService;
            _trayContextMenuService = trayContextMenuService;

            _trayContextMenuHelper = new TrayContextMenuHelper(_trayContextMenuService);

            int mySessionId = SessionUtils.GetCurrentSessionId();
            _namedPipeServer = new NamedPipesServer($"{ProductConstants.SHELL_EXTENSION_NAMED_PIPE_PREFIX}.{mySessionId}");
            _namedPipeServer.GotMessage += _namedPipeServer_GotMessage;
            _namedPipeServer.ErrorOccurred += _namedPipeServer_ErrorOccurred;

            if (_settingsService.GetSetting<bool>(SettingsKeys.IsDeskBandEnabled))
            {
                for (int i = 1; i <= 10; i++)
                {
                    try
                    {
                        ShowDeskBand();
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex is COMException || ex is Win32Exception)
                        {
                            if (i == 10)
                            {
                                _settingsService.SetSetting(SettingsKeys.IsDeskBandEnabled, false);
                                _settingsService.SetSetting(SettingsKeys.IsTrayIconEnabled, true);
                                throw;
                            }

                            Shell32.SHChangeNotify(NativeEnums.SHCNE.SHCNE_ASSOCCHANGED, NativeEnums.SHChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                            System.Threading.Thread.Sleep(500);
                        }
                    }
                }
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
                DeskbandUtils.AddDeskbandToTrayUsingUndocumentedTrayBandSiteService(_deskbandGuid);

                // The method below triggers a confirmation dialog, shown by Windows Explorer.
                // This condifmation dialog is not always on top of other windows, so the user
                // may not see it. I tried to add auto-confirmation function of the method ShowDeskBandd
                // but it does not always work, so I cannot rely on it. In the end it turns out that
                // relying on the undocumented interface TrayBandSiteService is more reliable
                // than using the official way, which causes this "not-always-easily-visible" dialog to appear.
                //DeskbandUtils.ShowDeskBand(_deskbandGuid, CLIPBAND_NAME, true);
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
            if (e.Key == SettingsKeys.IsDeskBandEnabled)
            {
                if ((bool)e.Value)
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
            _trayContextMenuHelper.Dispose();
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
            PipeMessage msg = PipeMessage.FromString(e.ReceivedMessage);
            switch (msg.Command)
            {
                case CMD_LEFT_CLICK:
                    ToggleMainWindowVisibility(msg.HWND);
                    break;
                case CMD_RIGHT_CLICK:
                    _trayContextMenuHelper.ToggleVisibility(msg.HWND);
                    break;
                default:
                    throw new InvalidProgramException($"Invalid pipe message: {e.ReceivedMessage}");
            }
        }

        /// <summary>
        /// Sometimes I get two WM_RBUTTONUP in a row, resulting from a single mouse click.
        /// It is much easier to handle this situation here, than in the DeskBand extension.
        /// </summary>
        private class TrayContextMenuHelper : IDisposable
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
                    // we do not have to dpi-callibrate these coordinates
                    var deskbandRect = WindowUtils.GetWindowRectangle(_lastDeskbandHwnd);

                    _trayContextMenuService.ShowContextMenuAtPoint(new Point
                    {
                        X = deskbandRect.X,
                        Y = deskbandRect.Y
                    });
                }
            }

            public void ToggleVisibility(IntPtr deskbandHwnd)
            {
                _lastDeskbandHwnd = deskbandHwnd;
                _duplicatedRightClickSligingTimer.Slide();
            }

            public void Dispose()
            {
                _duplicatedRightClickSligingTimer.Dispose();
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
