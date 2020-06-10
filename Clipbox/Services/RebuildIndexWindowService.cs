using Clipbox.Services.Interfaces;
using Clipbox.Views;
using System;
using System.ComponentModel;
using System.Windows;

namespace Clipbox.Services
{
    public class RebuildIndexWindowService : 
        IRebuildIndexWindowService, IDisposable
    {
        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private readonly ISearchService _searchService;

        private bool _disposed;
        private RebuildIndicesWindow _window;

        public RebuildIndexWindowService(ISearchService searchService, 
            IMainClipboardViewWindowService mainClipboardViewWindowService)
        {
            _searchService = searchService;
            _mainClipboardViewWindowService = mainClipboardViewWindowService;

            _mainClipboardViewWindowService.RebuildIndexRequested += _mainClipboardViewWindowService_RebuildIndexRequested;
            _mainClipboardViewWindowService.OpenMainWindowRequested += _mainClipboardViewWindowService_OpenMainWindowRequested;
        }

        private void _mainClipboardViewWindowService_OpenMainWindowRequested(object sender, CancelEventArgs e)
        {
            if (!_disposed && _window != null)
            {
                var window = _window;
                _window.Dispatcher.Invoke(() =>
                {
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = WindowState.Normal;

                    window.Focus();
                });
                
                e.Cancel = true;
            }
        }

        private void _mainClipboardViewWindowService_RebuildIndexRequested(object sender, EventArgs e)
        {
            if (!_disposed && _window == null)
            {
                // close the main window
                _mainClipboardViewWindowService.Close();

                // indicate that the rebuilding of the indices is running
                _window = new RebuildIndicesWindow(_searchService);
                _window.Closed += _window_Closed;
                _window.Show();
            }
        }

        private void _window_Closed(object sender, EventArgs e)
        {
            _window.Closed -= _window_Closed;
            _window = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_window?.IsVisible == true)
                {
                    _window?.Dispatcher.Invoke(() =>
                    {
                        _window?.Close();
                    });
                }

                _mainClipboardViewWindowService.RebuildIndexRequested -= _mainClipboardViewWindowService_RebuildIndexRequested;
                _disposed = true;
            }
        }
    }
}
