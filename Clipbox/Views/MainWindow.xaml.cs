using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Infrastructure.Extensibility.Containers;
using Clipbox.Infrastructure.Extensibility.Containers.Interfaces;
using Clipbox.Infrastructure.ExtentionMethods;
using Clipbox.Infrastructure.UI.ExtentionMethods;
using Clipbox.Infrastructure.UI.Theme;
using Clipbox.Infrastructure.UI.Windows;
using Clipbox.Services.Interfaces;
using Clipbox.Settings;
using Clipbox.Settings.Enums;
using Clipbox.ViewModels.PreviewRelatedObjects;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using Clipbox.Win32.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
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
        private IClipboardFormatPreviewExtentionContainer _clipboardFormatPreviewExtContainer;
        private UIExtentionContainer _uiExtContainer;

        public MainWindow(IClipboardManager clipboardManager, 
            IClipboardFormatPreviewExtentionContainer clipboardFormatPreviewExtContainer, 
            UIExtentionContainer uiExtContainer,
            ISettingsService settingsService, 
            MonitorUtils.DisplayMonitor monitor)
        {
            _clipboardManager = clipboardManager;
            _clipboardFormatPreviewExtContainer = clipboardFormatPreviewExtContainer;
            _uiExtContainer = uiExtContainer;
            _settingsService = settingsService;
            _monitor = monitor;

            if (!ThemeManager.IsLoaded)
            {
                ThemeManager.LoadThemeFromSystemTheme();
            }

            InitializeComponent();

            _mainViewClipboardContents = new MainViewClipboardContents(new ViewModels.MainClipboardViewViewModel(_clipboardManager, _clipboardFormatPreviewExtContainer, _settingsService), _uiExtContainer, settingsService);
            clipboardContents.Content = _mainViewClipboardContents;

            _backgroundRenderMode = (MainClipboardViewBackgroundRenderMode)_settingsService.GetSetting<int>(SettingsKeys.MainClipboardViewBackgroundRenderMode);

            ScaleMode = Infrastructure.UI.Windows.Enums.DpiScaleMode.TaggedChildren;

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
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            User32.SetForegroundWindow(Handle);
        }

        public new bool IsVisible { get; private set; }

        public void SetCurrentDisplayHeight(int height)
        {
            _mainViewClipboardContents.SetCurrentDisplayHeight(height);
        }

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
    }
    
}
