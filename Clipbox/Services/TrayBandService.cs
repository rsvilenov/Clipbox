using Clipbox.Core;
using Clipbox.Infrastructure.IPC;
using Clipbox.Infrastructure.Persistence.FilesystemAccess;
using Clipbox.Infrastructure.UI;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Clipbox.Services
{

    public class TrayBandService : ITrayBandService, IDisposable
    {
        private const string CMD_LEFT_CLICK = "LC";
        private const string CMD_RIGHT_CLICK = "RC";
        private const string CLIPBAND_NAME = "Clipbox"; //ProductConstants.PRODUCT_NAME;

        private readonly Guid _deskbandGuid = new Guid(ProductConstants.DESKBAND_SHELL_EXT_GUID);

        private readonly NamedPipesServer _namedPipeServer;
        private readonly TrayContextMenuHelper _trayContextMenuHelper;

        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private readonly ITrayContextMenuService _trayContextMenuService;
        private readonly ISettingsService _settingsService;
        private readonly IPauseService _pauseService;

        public TrayBandService(IMainClipboardViewWindowService mainClipboardViewWindowService,
            ITrayContextMenuService trayContextMenuService,
            ISettingsService settingsService,
            IPauseService pauseService)
        {
            _mainClipboardViewWindowService = mainClipboardViewWindowService;

            _settingsService = settingsService;
            _trayContextMenuService = trayContextMenuService;
            _pauseService = pauseService;

            _trayContextMenuHelper = new TrayContextMenuHelper(_trayContextMenuService);

            int mySessionId = SessionUtils.GetCurrentSessionId();
            _namedPipeServer = new NamedPipesServer($"{ProductConstants.SHELL_EXTENSION_NAMED_PIPE_PREFIX}.{mySessionId}");
            _namedPipeServer.GotMessage += _namedPipeServer_GotMessage;
            _namedPipeServer.ErrorOccurred += _namedPipeServer_ErrorOccurred;

            if (_settingsService.GetSetting<bool>(SettingsKeys.IsDeskBandEnabled))
            {
                ShowDeskBand(autoConfirm: true);
            }

            // manage the files, signalling the state to the deskband:
            // when we start, the paused state is impossible, so delete any leftover file from previous state signalling
            SetTrayBandSignalFileForPauseStateChanged(false);


            // get the "enabled" state and delete/create the signalling file accordingly
            bool isClipboardTrackingEnabled = _settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled);
            SetTrayBandFileForEnabledStateChanged(isClipboardTrackingEnabled);

            _settingsService.SettingChanged += _settingsService_SettingChanged;
            _pauseService.PauseStateChanged += _pauseService_PauseStateChanged;
        }

        private void _pauseService_PauseStateChanged(object sender, EventArgsClasses.PauseEventArgs e)
        {
            string disabledFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), "paused");
            if (e.IsPaused && e.PauseIntervalMs != -1)
            {
                SetTrayBandSignalFileForPauseStateChanged(true);
            }
            else if (File.Exists(disabledFileName))
            {
                SetTrayBandSignalFileForPauseStateChanged(false);
            }
        }

        private void DisableDeskband()
        {
            _settingsService.SetSetting(SettingsKeys.IsDeskBandEnabled, false);
            _settingsService.SetSetting(SettingsKeys.IsTrayIconEnabled, true);
        }

        private void _namedPipeServer_ErrorOccurred(object sender, NamedPipeErrorEventArgs e)
        {
            throw e.Exception;
        }

        private void ShowDeskBand(bool autoConfirm)
        {   
            string deskbandCtrlProcessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProductConstants.DESKBAND_CTRL_EXE_NAME);
            if (File.Exists(deskbandCtrlProcessPath))
            {
                using (Process deskbandCtlProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = deskbandCtrlProcessPath,
                        Arguments = autoConfirm ? "nowait" : string.Empty
                    }
                })
                {
                    deskbandCtlProcess.Start();
                    deskbandCtlProcess.WaitForExit();

                    if (deskbandCtlProcess.ExitCode != (int)DeskbandCtrlProcessExitCodes.OK)
                    {
                        DisableDeskband();
                    }
                }
            }
            else
            {
                try
                {
                    if (!DeskbandUtils.IsTaskBandShown(_deskbandGuid))
                    {
                        //DeskbandUtils.AddDeskbandToTrayUsingUndocumentedTrayBandSiteService(_deskbandGuid);
                        DeskbandUtils.ShowDeskBand(_deskbandGuid, CLIPBAND_NAME, autoConfirm);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is COMException || ex is Win32Exception)
                    {
                        DisableDeskband();
                        throw;
                    }
                }
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
                    ShowDeskBand(autoConfirm: true);
                }
                else
                {
                    HideDeskBand();
                }
            }

            if (e.Key == SettingsKeys.IsClipboardTrackingEnabled)
            {
                SetTrayBandFileForEnabledStateChanged((bool)e.Value);
            }
        }

        private void SetTrayBandFileForEnabledStateChanged(bool isEnabled)
        {
            // create this file in AppData so that the deskband can find it. 
            // IMPORTANT: Do not use IPersistenceProvider here, because its implementation may change,
            // and we want to store the filename precisely in %APPDATA%\Clipbox, so that the
            // deskband Shell extension, which is written in c++, can read it.
            string disabledFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), "disabled");
            if (isEnabled)
            {
                File.Delete(disabledFileName);
            }
            else
            {
                File.Create(disabledFileName).Dispose();
            }
        }

        private void SetTrayBandSignalFileForPauseStateChanged(bool isPaused)
        {
            string disabledFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), "paused");
            if (isPaused)
            {
                File.Create(disabledFileName).Dispose();
            }
            else if (File.Exists(disabledFileName))
            {
                File.Delete(disabledFileName);
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


        /// <summary>
        /// Process exit codes of ClipboxDeskbandControl.exe
        /// The same enum exists in ClipboxDeskbandControl.cpp
        /// </summary>
        public enum DeskbandCtrlProcessExitCodes
        {
            OK,
            DeskbandNotRegistered,
            DeskbandFailedToShow,
            Unknown
        }
    }
}
