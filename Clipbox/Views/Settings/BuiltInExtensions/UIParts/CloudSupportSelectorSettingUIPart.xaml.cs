using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for CloudSupportSelectorSettingUIPart.xaml
    /// </summary>
    public partial class CloudSupportSelectorSettingUIPart : SettingUIPart<string>, IDisposable
    {
        private bool _isDisposed;

        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }
        public ICommand ChangeClicked { get; }
        public ICommand RetryClicked { get; }
        public ICommand ShowDetailsClicked { get; }
        public ICommand CopyDetailsToClipboardClicked { get; }
        public ICommand ClearCloudDataClicked { get; }

        private ICloudSynchronizationService _cloudSyncSvc;

        public CloudSupportSelectorSettingUIPart()
        {
            InitializeComponent();

#if DEBUG
            IsInDebugMode = true;
#endif

            AvailableCloudExtensions = new AsyncObservableCollection<CloudInfoDisplayData>();
            this.mainGrid.DataContext = this;
            this.Visibility = Visibility.Collapsed;
            IsVisibleChanged += CloudSupportSelectorSettingUIPart_IsVisibleChanged;

            OkClicked = new DelegateCommand(OkCommandHandler, () => AvailableCloudExtensions.Any() && SelectedCloudInfoDisplayData != null);
            CancelClicked = new DelegateCommand(CancelCommandHandler);
            ChangeClicked = new DelegateCommand(ChangeClickedHandler, () => AvailableCloudExtensions.Any());
            RetryClicked = new DelegateCommand(RetryClickedHandler);
            ShowDetailsClicked = new DelegateCommand(ShowDetailsHandler);
            CopyDetailsToClipboardClicked = new DelegateCommand(CopyDetailsToClipboardHandler);
            ClearCloudDataClicked = new DelegateCommand(ClearCloudDataHandler);
        }

        private void ClearCloudDataHandler()
        {
            var mainSyncContext = SynchronizationContext.Current;

            IsClearDataRunning = true;
            Task.Run(() =>
            {
                try
                {
                    _cloudSyncSvc.ClearSyncedData();
                }
                catch (Exception ex)
                {
                    var edi = ExceptionDispatchInfo.Capture(ex);
                    mainSyncContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                }
                finally
                {
                    Dispatcher.Invoke(() => IsClearDataRunning = false);
                }
            });
        }

        private void CopyDetailsToClipboardHandler()
        {
            Clipboard.SetText(ErrorDetailsForPopup);
        }

        private void ShowDetailsHandler()
        {
            ErrorDetailsForPopup = ErrorDetails;
            errorDetailsPopup.IsOpen = true;
        }

        private void RetryClickedHandler()
        {
            _cloudSyncSvc.RequestAuthentication();
        }
        private void ChangeClickedHandler()
        {
            cloudSelectorPopup.IsOpen = true;
        }

        private void CancelCommandHandler()
        {
            if (string.IsNullOrEmpty(CloudExtensionName))
            {
                SetIsVisible(false);
                SelectedCloudInfoDisplayData = null;
            }

            cloudSelectorPopup.IsOpen = false;
        }

        private void OkCommandHandler()
        {
            CloudExtensionName = SelectedCloudInfoDisplayData.Name;
            CloudExtensionIcon = SelectedCloudInfoDisplayData.Icon;

            cloudSelectorPopup.IsOpen = false;
        }

        private void CloudSupportSelectorSettingUIPart_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                LoadCloudExtensionsInfo();

                if (AvailableCloudExtensions.Any())
                {
                    if (string.IsNullOrEmpty(CloudExtensionName) || !AvailableCloudExtensions.Any(ace => ace.Name == CloudExtensionName))
                    {
                        cloudSelectorPopup.IsOpen = true;
                        cloudSelectorPopup.Closed += CloudSelectorPopup_Closed;
                        Window.GetWindow(this).IsEnabled = false;
                    }
                    else
                    {
                        SelectedCloudInfoDisplayData = AvailableCloudExtensions.First(ace => ace.Name == CloudExtensionName);
                        CloudExtensionIcon = SelectedCloudInfoDisplayData.Icon;
                    }
                }
            }
        }

        private void CloudSelectorPopup_Closed(object sender, System.EventArgs e)
        {
            Window.GetWindow(this).IsEnabled = true;
        }

        private void LoadCloudExtensionsInfo()
        {
            AvailableCloudExtensions.Clear();

            IEnumerable<Type> availableExtensionTypes = _cloudSyncSvc.GetAvailableExtensionTypes();

            foreach (Type extType in availableExtensionTypes)
            {
                if (!(CloudSupportAttribute.IsDefinedFor(extType) 
                    && ClipboxExtensionAttribute.IsDefinedFor(extType)))
                {
                    continue;
                }

                ICloudInfo cloudInfo = CloudSupportAttribute.GetCloudInfo(extType);
                ImageSource imgSource = null;
                if (cloudInfo.IconImageData != null)
                {
                    using (MemoryStream ms = new MemoryStream(cloudInfo.IconImageData))
                    using (Bitmap bmp = new Bitmap(ms))
                    {
                        imgSource = bmp.ToBitmapSource();
                    }
                }

                IExtensionInfo extInfo = ClipboxExtensionAttribute.GetExtensionInfo(extType);
                AvailableCloudExtensions.Add(new CloudInfoDisplayData
                {
                    Description = extInfo.Description,
                    Name = cloudInfo.CloudServiceName,
                    Icon = imgSource
                });
            }
        }

        public bool IsInDebugMode
        {
            get { return (bool)GetValue(IsInDebugModeProperty); }
            set { SetValue(IsInDebugModeProperty, value); }
        }

        public static readonly DependencyProperty IsInDebugModeProperty =
            DependencyProperty.Register(nameof(IsInDebugMode), typeof(bool), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(false));



        public bool IsClearDataRunning
        {
            get { return (bool)GetValue(IsClearDataRunningProperty); }
            set { SetValue(IsClearDataRunningProperty, value); }
        }

        public static readonly DependencyProperty IsClearDataRunningProperty =
            DependencyProperty.Register(nameof(IsClearDataRunning), typeof(bool), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(false));



        private AsyncObservableCollection<CloudInfoDisplayData> AvailableCloudExtensions
        {
            get { return (AsyncObservableCollection<CloudInfoDisplayData>)GetValue(AvailableCloudExtensionsProperty); }
            set { SetValue(AvailableCloudExtensionsProperty, value); }
        }

        private static readonly DependencyProperty AvailableCloudExtensionsProperty =
            DependencyProperty.Register(nameof(AvailableCloudExtensions), typeof(AsyncObservableCollection<CloudInfoDisplayData>), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(null));



        private CloudInfoDisplayData SelectedCloudInfoDisplayData
        {
            get { return (CloudInfoDisplayData)GetValue(SelectedCloudInfoDisplayDataProperty); }
            set { SetValue(SelectedCloudInfoDisplayDataProperty, value); }
        }

        private static readonly DependencyProperty SelectedCloudInfoDisplayDataProperty =
            DependencyProperty.Register(nameof(SelectedCloudInfoDisplayData), typeof(CloudInfoDisplayData), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(null));




        public ImageSource CloudExtensionIcon
        {
            get { return (ImageSource)GetValue(CloudExtensionIconconProperty); }
            set { SetValue(CloudExtensionIconconProperty, value); }
        }

        public static readonly DependencyProperty CloudExtensionIconconProperty =
            DependencyProperty.Register(nameof(CloudExtensionIcon), typeof(ImageSource), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(null));



        public string CloudExtensionName
        {
            get { return (string)GetValue(CloudExtensionNameProperty); }
            set { SetValue(CloudExtensionNameProperty, value); }
        }

        public static readonly DependencyProperty CloudExtensionNameProperty =
            DependencyProperty.Register(nameof(CloudExtensionName), typeof(string), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(string.Empty, CloudExtensionName_PropertyChanged));

        private static void CloudExtensionName_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (CloudSupportSelectorSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override string GetSettingValue()
        {
            return CloudExtensionName;
        }

        public override void SetSettingValue(string value)
        {
            CloudExtensionName = value;
            if (AvailableCloudExtensions.Any() && !string.IsNullOrEmpty(CloudExtensionName))
            {
                SelectedCloudInfoDisplayData = AvailableCloudExtensions.First(ace => ace.Name == CloudExtensionName);
                CloudExtensionIcon = SelectedCloudInfoDisplayData.Icon;
            }
        }


        private class CloudInfoDisplayData : BindableObject
        {
            private string _name;
            public string Name
            {
                get { return _name; }
                set { SetField(ref _name, value); }
            }

            private string _description;
            public string Description
            {
                get { return _description; }
                set { SetField(ref _description, value); }
            }

            private ImageSource _icon;
            public ImageSource Icon
            {
                get { return _icon; }
                set { SetField(ref _icon, value); }
            }

        }




        public void SetCloudSyncService(ICloudSynchronizationService cloudSyncSvc)
        {
            _cloudSyncSvc = cloudSyncSvc;

            if (_cloudSyncSvc.IsInErrorState)
            {
                ConnectionState = CloudServiceConnectionState.Error;
                ErrorMessage = _cloudSyncSvc.GetLastError();
                ErrorDetails = _cloudSyncSvc.GetLastErrorDetails();
            }
            else
            {
                ConnectionState = DetermineConnectionState();
            }

            _cloudSyncSvc.AuthenticationStateChanged += _cloudSyncSvc_AuthenticationStateChanged;
            _cloudSyncSvc.NetworkAvailabilityChanged += _cloudSyncSvc_NetworkAvailabilityChanged;
            _cloudSyncSvc.ErrorStateChanged += _cloudSyncSvc_ErrorStateChanged;
        }

        private CloudServiceConnectionState DetermineConnectionState()
        {
            if (!_cloudSyncSvc.IsNetworkAvailable)
            {
                return CloudServiceConnectionState.NoNetwork;
            }
            else
                if (_cloudSyncSvc.IsAuthenticated)
            {
                return CloudServiceConnectionState.Authenticated;
            }
            else
                if (_cloudSyncSvc.IsAuthenticating)
            {
                return CloudServiceConnectionState.Authenticating;
            }
            else 
                if (_cloudSyncSvc.IsInErrorState)
            {
                return CloudServiceConnectionState.Error;
            }
            else
            {
                return CloudServiceConnectionState.Connecting;
            }
        }

        private void _cloudSyncSvc_ErrorStateChanged(object sender, ErrorStateEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.State == Services.Enums.ErrorState.Error)
                {
                    ConnectionState = CloudServiceConnectionState.Error;
                    ErrorMessage = e.Message;
                    ErrorDetails = e.Details;
                }
                else
                {
                    ConnectionState = DetermineConnectionState();
                }
            });
        }

        private void _cloudSyncSvc_NetworkAvailabilityChanged(object sender, System.EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_cloudSyncSvc.IsNetworkAvailable)
                {
                    if (ConnectionState == CloudServiceConnectionState.NoNetwork)
                    {
                        if (_cloudSyncSvc.IsAuthenticated)
                        {
                            ConnectionState = CloudServiceConnectionState.Authenticated;
                        }
                        else if (_cloudSyncSvc.IsAuthenticating)
                        {
                            ConnectionState = CloudServiceConnectionState.Authenticating;
                        }
                    }
                    else
                    {
                        ConnectionState = CloudServiceConnectionState.Connecting;
                    }
                }
                else
                {
                    ConnectionState = CloudServiceConnectionState.NoNetwork;
                }
            });
        }

        private void _cloudSyncSvc_AuthenticationStateChanged(object sender, AuthenticationStageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_cloudSyncSvc.IsAuthenticated)
                {
                    ConnectionState = CloudServiceConnectionState.Authenticated;
                }
                else if (_cloudSyncSvc.IsAuthenticating)
                {
                    ConnectionState = CloudServiceConnectionState.Authenticating;
                }
            });
        }

        public void Dispose()
        {
            if (_cloudSyncSvc != null && !_isDisposed)
            {
                _cloudSyncSvc.AuthenticationStateChanged -= _cloudSyncSvc_AuthenticationStateChanged;
                _cloudSyncSvc.NetworkAvailabilityChanged -= _cloudSyncSvc_NetworkAvailabilityChanged;
                _cloudSyncSvc.ErrorStateChanged -= _cloudSyncSvc_ErrorStateChanged;

                _isDisposed = true;
            }
        }

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(string.Empty));



        public string ErrorDetails
        {
            get { return (string)GetValue(ErrorDetailsProperty); }
            set { SetValue(ErrorDetailsProperty, value); }
        }

        public static readonly DependencyProperty ErrorDetailsProperty =
            DependencyProperty.Register(nameof(ErrorDetails), typeof(string), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(string.Empty));



        public string ErrorDetailsForPopup
        {
            get { return (string)GetValue(ErrorDetailsForPopupProperty); }
            set { SetValue(ErrorDetailsForPopupProperty, value); }
        }

        public static readonly DependencyProperty ErrorDetailsForPopupProperty =
            DependencyProperty.Register(nameof(ErrorDetailsForPopup), typeof(string), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(string.Empty));




        public CloudServiceConnectionState ConnectionState
        {
            get { return (CloudServiceConnectionState)GetValue(ConnectionStateProperty); }
            set { SetValue(ConnectionStateProperty, value); }
        }

        public static readonly DependencyProperty ConnectionStateProperty =
            DependencyProperty.Register(nameof(ConnectionState), typeof(CloudServiceConnectionState), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(CloudServiceConnectionState.Connecting));

    }

    public enum CloudServiceConnectionState
    {
        Connecting,
        Authenticating,
        Authenticated,
        NoNetwork,
        Error
    }
}
