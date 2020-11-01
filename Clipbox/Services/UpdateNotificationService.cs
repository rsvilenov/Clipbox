using Clipbox.Core;
using Clipbox.Infrastructure.RegistryAccess;
using Clipbox.Services.Interfaces;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Timers;

namespace Clipbox.Services
{
    public class UpdateNotificationService : IUpdateNotificationService, IDisposable
    {
        private const int UPDATE_CHECK_INTERVAL_MS = 10 * 60 * 1000; // 10 min
        public bool IsUpdateAvailable { get; private set; }

        public event EventHandler UpdateAvailable;

        private Timer _updateCheckTimer;
        private bool _isNetworkAvailable;
        private readonly WebClient _webClient;
        private bool _isDisposed;

        public UpdateNotificationService()
        {
            _isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            _webClient = new WebClient();

            _updateCheckTimer = new Timer
            {
                Interval = UPDATE_CHECK_INTERVAL_MS,
                AutoReset = true
            };

            _updateCheckTimer.Elapsed += _updateCheckTimer_Elapsed;

            if (_isNetworkAvailable)
            {
                _updateCheckTimer.Start();
            }
        }

        public void LaunchUpdate()
        {
            string cachedSetupPath = RegistryUtils.GetBundleCachePath();
            if (!string.IsNullOrEmpty(cachedSetupPath))
            {
                Process.Start(cachedSetupPath, "forceupdatedownload");
            }
            else
            {
                Process.Start(ProductConstants.PRODUCT_UPDATE_PAGE_URL);
            }
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            _isNetworkAvailable = e.IsAvailable;

            if (_isNetworkAvailable)
            {
                _updateCheckTimer.Start();
            }
            else
            {
                _updateCheckTimer.Stop();
            }
        }

        private void _updateCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isNetworkAvailable)
            {
                // if we run another download while the previous is running, the WebClient will throw an exception
                _updateCheckTimer.Stop();

                try
                {
                    _webClient.DownloadStringAsync(new Uri(ProductConstants.PRODUCT_LATEST_VERSION_URL));
                }
                catch
                {
                    // TODO: log the exception
                }
            }
        }

        private void WebClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string newVersionStr = e.Result;

                if (!string.IsNullOrEmpty(newVersionStr)
                    && double.TryParse(newVersionStr.Trim(), out double newVersion) 
                    && newVersion > ProductConstants.PRODUCT_VERSION)
                {
                    IsUpdateAvailable = true;
                    UpdateAvailable?.Invoke(this, new EventArgs());
                }
            }

            // stop checking for updates once a new version is found to be available
            if (!IsUpdateAvailable)
            {
                _updateCheckTimer.Start();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _updateCheckTimer.Dispose();
                _webClient.Dispose();

                _isDisposed = true;
            }
        }
    }
}
