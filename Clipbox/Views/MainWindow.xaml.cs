using Clipbox.Core.Extensibility.Contracts;
using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.UI.DataObjects;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.Infrastructure.UI.Theme;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Settings.Enums;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AcryllicWindow, IMainWindow
    {
        private ISettingsService _settingsService;
        
        private WallpaperWindow _wallpaperWindow;

        private MonitorUtils.DisplayMonitor _monitor;

        private MainClipboardViewBackgroundRenderMode _backgroundRenderMode;

        private MainViewClipboardContents _mainViewClipboardContents;

        private IClipboardManager _clipboardManager;
        private UIExtentionContainer _uiExtContainer;
        
        public event EventHandler<CombinabilityEventArgs> ClipbardItemsCombinabilityCheckRequested;

        public event EventHandler<SetToClipboardEventArgs> SetItemsToClipboardRequested;

        public event EventHandler<HousekeepingEventArgs> HousekeepingRequested;

        public event EventHandler CloseRequested;
        
        /// <summary>
        /// A static constructor, used solely for overriding metadata
        /// </summary>
        static MainWindow()
        {
            // reduce the framerate of the animations, so that they do not use a lot of CPU power.
            // This framerate corresponds to MainWindowAnimationsFrameRate setting, which is a static setting,
            // which requires application restart upon change.
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline),
               new FrameworkPropertyMetadata { DefaultValue = 60 });
        }

        public MainWindow(IClipboardManager clipboardManager,
            IPreviewGenerationService previewGenerationService,
            UIExtentionContainer uiExtContainer,
            ISettingsService settingsService, 
            MonitorUtils.DisplayMonitor monitor)
        {
            _clipboardManager = clipboardManager;
            _uiExtContainer = uiExtContainer;
            _settingsService = settingsService;
            _monitor = monitor;

            if (!ThemeManager.IsLoaded)
            {
                ThemeManager.LoadThemeFromSystemTheme();
            }

            InitializeComponent();

            _mainViewClipboardContents = new MainViewClipboardContents(new ViewModels.MainViewClipboardContentsViewModel(_clipboardManager, previewGenerationService, _settingsService), _uiExtContainer, settingsService);
            clipboardContents.Content = _mainViewClipboardContents;
            
            _mainViewClipboardContents.CombinabilityCheckRequested += _mainViewClipboardContents_CombinabilityCheckRequested;
            _mainViewClipboardContents.SetItemsToClipboardRequested += _mainViewClipboardContents_SetItemsToClipboardRequested;
            _mainViewClipboardContents.HousekeepingRequested += _mainViewClipboardContents_HousekeepingRequested;
            _mainViewClipboardContents.CloseMainWindowRequested += _mainViewClipboardContents_CloseMainWindowRequested;

            _backgroundRenderMode = (MainClipboardViewBackgroundRenderMode)_settingsService.GetSetting<int>(SettingsKeys.MainClipboardViewBackgroundRenderMode);

            ScaleMode = Infrastructure.UI.Windows.Enums.DpiScaleMode.Custom;

            // ensure the window will create itself invisible
            Width = 0;
            Height = 0;
            WindowStyle = WindowStyle.None;
            ShowInTaskbar = false;
            ShowActivated = false;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual;
            if (_backgroundRenderMode == MainClipboardViewBackgroundRenderMode.GDI)
            {
                AllowsTransparency = true;
                Background = System.Windows.Media.Brushes.Transparent;
                base.Show();
            }
            else if (_backgroundRenderMode == MainClipboardViewBackgroundRenderMode.Acryllic)
            {
                if (!ThemeManager.IsAcryllicTransparencyEnabled)
                {
                    throw new InvalidOperationException("The system trasparency settings do not allow acryllic blur.");
                }

                Background = System.Windows.Media.Brushes.Black;
                IsBlurEnabled = true;
            }

            Closing += MainWindow_Closing;
            Shown += MainWindow_Shown;
            KeyUp += MainWindow_KeyUp;
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseRequested?.Invoke(this, new EventArgs());
            }
        }

        private void _mainViewClipboardContents_CloseMainWindowRequested(object sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, e);
        }

        private void _mainViewClipboardContents_HousekeepingRequested(object sender, HousekeepingEventArgs e)
        {
            HousekeepingRequested?.Invoke(this, e);
        }

        private void _mainViewClipboardContents_SetItemsToClipboardRequested(object sender, SetToClipboardEventArgs e)
        {
            SetItemsToClipboardRequested?.Invoke(this, e);
        }

        private void _mainViewClipboardContents_CombinabilityCheckRequested(object sender, CombinabilityEventArgs e)
        {
            ClipbardItemsCombinabilityCheckRequested?.Invoke(this, e);
        }
        
        private void MainWindow_Shown(object sender, EventArgs e)
        {
            User32.SetForegroundWindow(Handle);
        }

        public new bool IsVisible { get; private set; }

        public void SetMonitorDimensions(MonitorDimensions dimensions)
        {
            _mainViewClipboardContents.SetMonitorDimensions(dimensions);
        }

        public new void Show()
        {
            if (_backgroundRenderMode == MainClipboardViewBackgroundRenderMode.GDI)
            {
                EmbedInWallpaperParent(_monitor);
            }
            else
            {
                base.Show();
                WindowState = WindowState.Maximized;
                IsVisible = true;
            }
        }
        
        public new void Close()
        {
            if (_backgroundRenderMode == MainClipboardViewBackgroundRenderMode.GDI)
            {
                DetachFromWallpaperParent();
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                Background = null;
                base.Close();
                IsVisible = false;
            }));
        }

        private void EmbedInWallpaperParent(MonitorUtils.DisplayMonitor monitor)
        {

            _wallpaperWindow = new WallpaperWindow(monitor);
            _wallpaperWindow.WaitContentPainted();

            var rect = WindowUtils.GetWindowRectangle(_wallpaperWindow.Handle);


            Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                //base.Hide();

                //Width = rect.Width / ScaleFactor;
                //Height = rect.Height / ScaleFactor;
                //Top = rect.Top;
                //Left = rect.Left;

                //SetDesktopWallpaperBackgoround(rect.Width, rect.Height);

                //base.Show();
                WindowState = WindowState.Maximized;
                User32.SetForegroundWindow(Handle);

                IsVisible = true;
            }));
        }
        
        private void DetachFromWallpaperParent()
        {
            if (_wallpaperWindow != null)
            {
                _wallpaperWindow.Dispose();
                _wallpaperWindow = null;
            }
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_backgroundRenderMode == MainClipboardViewBackgroundRenderMode.GDI)
            {
                DetachFromWallpaperParent();
            }

            IsVisible = false;

            this.Content = null;

            _mainViewClipboardContents.Dispose();
        }

        private void SetDesktopWallpaperBackgoround(int width, int height)
        {
            using (Bitmap wallpaperBmp = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(wallpaperBmp))
            {
                IntPtr hdc = g.GetHdc();

                try
                {
                    if (!User32.PaintDesktop(hdc))
                    {
                        throw new Win32Exception();
                    }
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }

                var wallpaperBrusgh = new ImageBrush(wallpaperBmp.ToBitmapSource());
                wallpaperBrusgh.Freeze();
                this.Background = wallpaperBrusgh;
            }
        }

        protected override void OnSizeOrPositionChanging(ref System.Windows.Point position, ref System.Windows.Size size)
        {
            // when there are no window styles set the maximized window covers the taskbar
            // and we do not want that, so we have to do the following:

            // https://blogs.msdn.microsoft.com/llobo/2006/08/01/maximizing-window-with-windowstylenone-considering-taskbar/

            position.X = Math.Abs(_monitor.WorkingArea.Left - _monitor.Bounds.Left);
            position.Y = Math.Abs(_monitor.WorkingArea.Top - _monitor.Bounds.Top);
            size.Width = Math.Abs(_monitor.WorkingArea.Right - _monitor.Bounds.Left);
            size.Height = Math.Abs(_monitor.WorkingArea.Bottom - _monitor.Bounds.Top);

            if (_backgroundRenderMode == MainClipboardViewBackgroundRenderMode.ImageBrush && !(Background is ImageBrush))
            {
                SetDesktopWallpaperBackgoround((int)size.Width, (int)size.Height);
            }
        }

        ~MainWindow()
        {
            Debug.WriteLine("GC MainWindow");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        public IReadOnlyList<IFlowParameter> GetFlowParameters()
        {
            return _mainViewClipboardContents.GetFlowParameters();
        }
    }
    
}
