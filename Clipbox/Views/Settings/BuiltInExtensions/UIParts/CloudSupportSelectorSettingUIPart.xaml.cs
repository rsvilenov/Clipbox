using Clipbox.Core;
using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Infrastructure.Extensibility.Loading;
using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Infrastructure.UI.ExtensionMethods;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for CloudSupportSelectorSettingUIPart.xaml
    /// </summary>
    public partial class CloudSupportSelectorSettingUIPart : SettingUIPart<string>
    {
        public ICommand OkClicked { get; }
        public ICommand CancelClicked { get; }
        public ICommand ChangeClicked { get; }

        public ICommand RetryClicked { get; }

        private ICloudSynchronizationService _cloudSyncSvc;

        public CloudSupportSelectorSettingUIPart()
        {
            InitializeComponent();

            AvailableCloudExtensions = new AsyncObservableCollection<CloudInfoDisplayData>();
            this.mainGrid.DataContext = this;
            this.Visibility = Visibility.Collapsed;
            IsVisibleChanged += CloudSupportSelectorSettingUIPart_IsVisibleChanged;

            OkClicked = new DelegateCommand(OkCommandHandler, () => AvailableCloudExtensions.Any() && SelectedCloudInfoDisplayData != null);
            CancelClicked = new DelegateCommand(CancelCommandHandler);
            ChangeClicked = new DelegateCommand(ChangeClickedHandler, () => AvailableCloudExtensions.Any());
            RetryClicked = new DelegateCommand(RetryClickedHandler);


            Unloaded += CloudSupportSelectorSettingUIPart_Unloaded;
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
            }
            else
            if (!_cloudSyncSvc.IsNetworkAvailable)
            {
                ConnectionState = CloudServiceConnectionState.NoNetwork;
            }
            else
            if (_cloudSyncSvc.IsAuthenticated)
            {
                ConnectionState = CloudServiceConnectionState.Authenticated;
            }
            else
            if (_cloudSyncSvc.IsAuthenticating)
            {
                ConnectionState = CloudServiceConnectionState.Authenticating;
            }

            _cloudSyncSvc.AuthenticationStateChanged += _cloudSyncSvc_AuthenticationStateChanged;
            _cloudSyncSvc.NetworkAvailabilityChanged += _cloudSyncSvc_NetworkAvailabilityChanged;
            _cloudSyncSvc.ErrorOccurred += _cloudSyncSvc_ErrorOccurred;
        }

        private void _cloudSyncSvc_ErrorOccurred(object sender, System.IO.ErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionState = CloudServiceConnectionState.Error;
                ErrorMessage = _cloudSyncSvc.GetLastError();
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

        private void CloudSupportSelectorSettingUIPart_Unloaded(object sender, RoutedEventArgs e)
        {
            _cloudSyncSvc.AuthenticationStateChanged -= _cloudSyncSvc_AuthenticationStateChanged;
            _cloudSyncSvc.NetworkAvailabilityChanged -= _cloudSyncSvc_NetworkAvailabilityChanged;
            _cloudSyncSvc.ErrorOccurred -= _cloudSyncSvc_ErrorOccurred;
        }

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(CloudSupportSelectorSettingUIPart), new PropertyMetadata(string.Empty));


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
