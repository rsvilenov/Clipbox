using Clipbox.Infrastructure.UI.Theme;
using Clipbox.Infrastructure.UI.Utils;
using Clipbox.ViewModels;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for TrayContextMenu.xaml
    /// </summary>
    public partial class TrayContextMenu : UserControl, ITrayContextMenu, IDisposable
    {
        private readonly TrayContextMenuViewModel _viewModel;
        private ContextMenu _trayContextMenu;

        private bool _isDisposed;

        public event EventHandler Opened;

        public TrayContextMenu(TrayContextMenuViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            InitializeComponent();

            if (!ThemeManager.IsLoaded)
            {
                ThemeManager.LoadThemeFromSystemTheme();
            }

        }

        private void _trayContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            _trayContextMenu.Opened -= _trayContextMenu_Opened;

            HwndSource contextMenuHWndSource = (HwndSource)HwndSource.FromVisual(_trayContextMenu);
            if (contextMenuHWndSource != null)
            {
                Handle = contextMenuHWndSource.Handle;
                CurrentMonitor = MonitorUtils.GetByWindowHandle(Handle);
                System.Drawing.Rectangle rect = WindowUtils.GetWindowRectangle(this.Handle);
                WindowUtils.SetWindowsPosition(this.Handle,
                    _requestedPositionX - rect.Width,
                    _requestedPositionY - rect.Height,
                    rect.Width,
                    rect.Height);

            }

            Opened?.Invoke(this, new EventArgs());
        }

        public IntPtr Handle { get; private set; }

        public MonitorUtils.DisplayMonitor CurrentMonitor { get; private set; }

        private int _requestedPositionX;
        private int _requestedPositionY;
        public void OpenAtPoint(Point p, double scaleFactor)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            _trayContextMenu = (ContextMenu)this.FindResource("TrayContextMenu");
            _trayContextMenu.Opened += _trayContextMenu_Opened;

            _trayContextMenu.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                _trayContextMenu.DataContext = this.DataContext;
                
                _trayContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                //_trayContextMenu.HorizontalOffset = p.X;
                //_trayContextMenu.VerticalOffset = p.Y;
                _requestedPositionX = (int)(p.X * scaleFactor);
                _requestedPositionY = (int)(p.Y * scaleFactor);

                _trayContextMenu.IsOpen = true;
                _trayContextMenu.StaysOpen = false;
            }));
        }

        public void Close()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            _trayContextMenu.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                _trayContextMenu.IsOpen = false;
            }));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {

                if (_viewModel is IDisposable disposableVm)
                {
                    disposableVm.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
