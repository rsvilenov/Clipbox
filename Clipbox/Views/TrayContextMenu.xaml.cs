using Clipbox.Infrastructure.UI.Theme;
using Clipbox.ViewModels;
using Clipbox.Views.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Clipbox.Views
{
    /// <summary>
    /// Interaction logic for TrayContextMenu.xaml
    /// </summary>
    public partial class TrayContextMenu : UserControl, ITrayContextMenu
    {
        private TrayContextMenuViewModel _viewModel;

        private ContextMenu _trayContextMenu;

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

            _trayContextMenu.Opened += (s, a) => Opened?.Invoke(s, a);
        }

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
    }
}
