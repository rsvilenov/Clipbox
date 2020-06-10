using Clipbox.ExtendedClipboard.Interfaces;
using Clipbox.Services.Interfaces;
using Clipbox.Views;
using Clipbox.Views.EventArgsClasses;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Clipbox.Services
{
    public class RebuildIndexWindowService : 
        IRebuildIndexWindowService, IDisposable
    {
        private readonly IMainClipboardViewWindowService _mainClipboardViewWindowService;
        private readonly ISearchService _searchService;
        private readonly IClipboardManager _clipboardManager;

        private bool _disposed;
        private RebuildIndicesWindow _window;

        public RebuildIndexWindowService(ISearchService searchService, 
            IMainClipboardViewWindowService mainClipboardViewWindowService,
            IClipboardManager clipboardManager)
        {
            _searchService = searchService;
            _mainClipboardViewWindowService = mainClipboardViewWindowService;
            _clipboardManager = clipboardManager;

            _mainClipboardViewWindowService.RebuildIndexRequested += _mainClipboardViewWindowService_RebuildIndexRequested;
            _mainClipboardViewWindowService.OpenMainWindowRequested += _mainClipboardViewWindowService_OpenMainWindowRequested;
        }

        private void _mainClipboardViewWindowService_OpenMainWindowRequested(object sender, CancelEventArgs e)
        {
            if (!_disposed && _window != null)
            {
                var window = _window;
                window.Dispatcher.Invoke(() =>
                {
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = WindowState.Normal;
                    window.Activate();
                });
                
                e.Cancel = true;
            }
        }

        private void _mainClipboardViewWindowService_RebuildIndexRequested(object sender, EventArgs e)
        {
            StartRebuildIndex();
        }

        private void StartRebuildIndex()
        {
            if (!_disposed && _window == null)
            {
                // close the main window
                _mainClipboardViewWindowService.Close();

                // indicate that the rebuilding of the indices is running
                _window = new RebuildIndicesWindow(_searchService);
                _window.Unloaded += _window_Unloaded;
                _window.RetryRequested += _window_RetryRequested;
                _window.Show();
            }
        }

        private void _window_RetryRequested(object sender, RetryRebuildIndexEventArgs e)
        {
            string newestItemId = _clipboardManager.GetAllItemIds().FirstOrDefault();
            if (string.IsNullOrEmpty(newestItemId))
            {
                _clipboardManager.RemoveClipboardItem(newestItemId);
            }
            
            _window?.RetryRebuild();
        }

        private void _window_Unloaded(object sender, EventArgs e)
        {
            _window.Closed -= _window_Unloaded;
            _window.RetryRequested -= _window_RetryRequested;
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
