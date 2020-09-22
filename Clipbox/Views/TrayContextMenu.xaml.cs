using Clipbox.Infrastructure.UI.Theme;
using Clipbox.ViewModels;
using Clipbox.Views.Interfaces;
using Clipbox.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for TrayContextMenu.xaml
    /// </summary>
    public partial class TrayContextMenu : UserControl, ITrayContextMenu, IDisposable
    {
        private readonly TrayContextMenuViewModel _viewModel;
        private readonly ContextMenu _trayContextMenu;

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

            _trayContextMenu = (ContextMenu)this.FindResource("TrayContextMenu");
            _trayContextMenu.Opened += _trayContextMenu_Opened;
        }

        private void _trayContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            Window contextMenuWindow = Window.GetWindow(_trayContextMenu);
            if (contextMenuWindow != null)
            {
                IntPtr windowHwnd = new WindowInteropHelper(contextMenuWindow).Handle;
                CurrentMonitor = MonitorUtils.GetByWindowHandle(windowHwnd);
            }

            Opened?.Invoke(this, new EventArgs());
        }

        public MonitorUtils.DisplayMonitor CurrentMonitor { get; private set; }

        public void OpenAtPoint(Point p)
        {
            _trayContextMenu.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
            {
                _trayContextMenu.DataContext = this.DataContext;

                _trayContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
                _trayContextMenu.HorizontalOffset = p.X;
                _trayContextMenu.VerticalOffset = p.Y;
                _trayContextMenu.IsOpen = true;
                _trayContextMenu.StaysOpen = false;
            }));
        }

        public void Close()
        {
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

                _trayContextMenu.Opened -= _trayContextMenu_Opened;

                _isDisposed = true;
            }
        }
    }
}
