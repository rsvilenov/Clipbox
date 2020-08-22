using Clipbox.Core;
using Clipbox.Infrastructure.ExtensionMethods;
using Clipbox.Services.Enums;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Views;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Infrastructure.UI.Resources;
using System.IO;
using Clipbox.Infrastructure.Persistence.FilesystemAccess;

namespace Clipbox.Services
{
    public class TrayIconService : ITrayIconService, IDisposable
    {
        private TaskbarIcon _taskbarIcon;

        private string _currentProcessName;

        public event EventHandler TrayIconClicked;

        // TODO: create a lightweight implementation of a message-only window
        // (a child of HWND_MESSAGE) and get rid of the HwndSource here as well as everywhere else
        private HwndSource _msgOnlyWindow;

        private uint _taskbarCreatedMessageId;

        private bool _isClipboardTrackingPaused;
        
        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private readonly ITrayContextMenuService _trayContextMenuService;
        private readonly ISettingsService _settingsService;
        private readonly IPauseService _pauseService;

        public TrayIconService(IMainClipboardViewWindowService mainClipboardViewWindowService, 
            ITrayContextMenuService trayContextMenuService,
            ISettingsService settingsService,
            IPauseService pauseService)
        {
            _mainClipboardViewWindowService = mainClipboardViewWindowService;
            _trayContextMenuService = trayContextMenuService;
            _settingsService = settingsService;
            _pauseService = pauseService;

            _taskbarIcon = new TaskbarIcon
            {
                Visibility = Visibility.Collapsed,
                ToolTipText = ProductConstants.PRODUCT_NAME
            };

            SetTrayIconImage();

            using (Process p = Process.GetCurrentProcess())
            {
                _currentProcessName = p.ProcessName;
            }

            _taskbarIcon.TrayLeftMouseUp += _taskbarIcon_TrayLeftMouseUp;
            _taskbarIcon.TrayRightMouseUp += _taskbarIcon_TrayRightMouseUp;

            _msgOnlyWindow = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, IntPtr.Zero);
            _msgOnlyWindow.AddHook(new HwndSourceHook(WndProc));

            _taskbarCreatedMessageId = Win32.Native.User32.RegisterWindowMessage("TaskbarCreated");

            UnfilterTaskbarCreatedMessage();

            if (_settingsService.GetSetting<bool>(SettingsKeys.IsTrayIconEnabled))
            {
                ShowTrayIcon();
            }

            _settingsService.SettingChanged += _settingsService_SettingChanged;
            _pauseService.PauseStateChanged += _pauseService_PauseStateChanged;
        }

        private void SetTrayIconImage()
        {
            using (Bitmap bmpIcon = ResourceUtils.GetApplicationIcon())
            using (Bitmap result = new Bitmap(Math.Max(bmpIcon.Width, bmpIcon.Width),
                         Math.Max(bmpIcon.Height, bmpIcon.Height)))
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmpIcon, System.Drawing.Point.Empty);


                if (_isClipboardTrackingPaused)
                {
                    using (var iconStream = Assembly.GetAssembly(this.GetType())
                            .GetManifestResourceStream("Clipbox.Resources.Images.TrayIcon.Pause.png"))
                    using (var pauseBmp = (Bitmap)Image.FromStream(iconStream))
                    {
                        g.DrawImage(pauseBmp, bmpIcon.Width / 2, bmpIcon.Height / 2, (int)(bmpIcon.Width / 1.5), (int)(bmpIcon.Height / 1.5));
                    }
                }
                else if (!_settingsService.GetSetting<bool>(SettingsKeys.IsClipboardTrackingEnabled))
                {
                    using (var iconStream = Assembly.GetAssembly(this.GetType())
                            .GetManifestResourceStream("Clipbox.Resources.Images.TrayIcon.Slash.png"))
                    using (var slashBmp = (Bitmap)Image.FromStream(iconStream))
                    {
                        g.DrawImage(slashBmp, 0, 0, bmpIcon.Width, bmpIcon.Height);
                    }
                }

                _taskbarIcon.Icon = result.ToIcon();
            }

        }

        private void _pauseService_PauseStateChanged(object sender, PauseEventArgs e)
        {
            string disabledFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), "paused");
            if (e.IsPaused && e.PauseIntervalMs != -1)
            {
                _isClipboardTrackingPaused = true;
                File.Create(disabledFileName).Dispose();
            }
            else if (File.Exists(disabledFileName))
            {
                _isClipboardTrackingPaused = false;
                File.Delete(disabledFileName);
            }

            SetTrayIconImage();
        }

        private void _settingsService_SettingChanged(object sender, SettingsEventArgs e)
        {
            if (e.Key == SettingsKeys.IsTrayIconEnabled)
            {
                if ((bool)e.Value)
                {
                    ShowTrayIcon();
                }
                else
                {
                    HideTrayIcon();
                }
            }
            else if (e.Key == SettingsKeys.IsClipboardTrackingEnabled)
            {
                // create this file in AppData so that the deskband can find it. 
                // IMPORTANT: Do not use IPersistenceProvider here, because its implementation may change,
                // and we want to store the filename precisely in %APPDATA%\Clipbox, so that the
                // deskband Shell extension, which is written in c++, can read it.
                string disabledFileName = Path.Combine(AppDataUtils.GetAppDataDir(false), "disabled");
                if ((bool)e.Value)
                {
                    File.Delete(disabledFileName);
                    _isClipboardTrackingPaused = false;
                }
                else
                {
                    File.Create(disabledFileName).Dispose();
                }

                SetTrayIconImage();
            }
        }

        private void _taskbarIcon_TrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            System.Drawing.Point p = new System.Drawing.Point();
            if (!User32.GetCursorPos(ref p))
            {
                throw new Win32Exception();
            }

            var monitor = MonitorUtils.GetByCurrentMousePosition();
            var scaleFactor = monitor.Dpi / 96;

            _trayContextMenuService.ShowContextMenuAtPoint(new System.Windows.Point
            {
                X = p.X / scaleFactor,
                Y = p.Y / scaleFactor
            });
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == _taskbarCreatedMessageId)
            {
                HideTrayIcon();
                ShowTrayIcon();
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Allow the non-elevated process of Explorer.exe to send the
        /// TaskbarCreated message to our process, which may run elevated.
        /// MSDN: https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-changewindowmessagefilterex
        /// </summary>
        private void UnfilterTaskbarCreatedMessage(bool throwOnFailure = false)
        {
            Win32.Native.NativeStructures.CHANGEFILTERSTRUCT changeFilterStruct = new Win32.Native.NativeStructures.CHANGEFILTERSTRUCT();
            changeFilterStruct.info = Win32.Native.NativeEnums.MessageFilterInfo.None;
            changeFilterStruct.size = (uint)Marshal.SizeOf<Win32.Native.NativeStructures.CHANGEFILTERSTRUCT>();

            bool filterChanged = Win32.Native.User32.ChangeWindowMessageFilterEx(_msgOnlyWindow.Handle, _taskbarCreatedMessageId, Win32.Native.NativeEnums.ChangeWindowMessageFilterExAction.Allow, ref changeFilterStruct);
            if (!filterChanged && throwOnFailure)
            {
                throw new Win32Exception();
            }
        }

        private void _taskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            if (_mainClipboardViewWindowService.IsOpen)
            {
                _mainClipboardViewWindowService.Close();
            }
            else
            {
                _mainClipboardViewWindowService.OpenOnMonitor(MonitorUtils.GetByCurrentMousePosition());
            }

            TrayIconClicked?.Invoke(this, new EventArgs());
        }
        
        public TrayIconVisibilityPreference GetVisibilityPreference()
        {
            Win32.TrayUtils.NotifyItemPreference? currentPreference = Win32.TrayUtils.GetTrayIconShowPreference(_currentProcessName);
            switch (currentPreference)
            {
                case Win32.TrayUtils.NotifyItemPreference.ShowAlways:
                    return TrayIconVisibilityPreference.ShowAlways;
                case Win32.TrayUtils.NotifyItemPreference.ShowNever:
                    return TrayIconVisibilityPreference.ShowNever;
                case Win32.TrayUtils.NotifyItemPreference.ShowWhenActive:
                    return TrayIconVisibilityPreference.ShowWhenActive;
                default:
                    throw new KeyNotFoundException("Could not find current preference.");
            }

        }

        public void HideTrayIcon()
        {
            _taskbarIcon.Visibility = Visibility.Hidden;
        }

        public void SetVisibilityPreference(TrayIconVisibilityPreference preference)
        {
            string processName = Process.GetCurrentProcess().ProcessName;

            TrayIconVisibilityPreference currentPreference = GetVisibilityPreference();
            
            if (currentPreference != preference)
            {
                Win32.TrayUtils.NotifyItemPreference niPreference = Win32.TrayUtils.NotifyItemPreference.ShowAlways;
                switch (preference)
                {
                    case TrayIconVisibilityPreference.ShowAlways:
                        niPreference = Win32.TrayUtils.NotifyItemPreference.ShowAlways;
                        break;
                    case TrayIconVisibilityPreference.ShowNever:
                        niPreference = Win32.TrayUtils.NotifyItemPreference.ShowNever;
                        break;
                    case TrayIconVisibilityPreference.ShowWhenActive:
                        niPreference = Win32.TrayUtils.NotifyItemPreference.ShowWhenActive;
                        break;
                }
                Win32.TrayUtils.SetTrayIconShowPreference(processName, niPreference);
            }
        }

        public void ShowTrayIcon()
        {
            _taskbarIcon.Visibility = Visibility.Visible;
        }

        public void ShowToolTip(string toolTipText)
        {
            // TODO: support rtf via custom tooltip
            _taskbarIcon.ShowBalloonTip("Clipbox", toolTipText, BalloonIcon.Info);
        }
        
        public void Dispose()
        {
            // TODO: do we really need to unsunscribe?
            _taskbarIcon.TrayLeftMouseUp -= _taskbarIcon_TrayLeftMouseUp;
            HideTrayIcon();
            _taskbarIcon.Dispose();
            _msgOnlyWindow.Dispose();
        }

    }
}
