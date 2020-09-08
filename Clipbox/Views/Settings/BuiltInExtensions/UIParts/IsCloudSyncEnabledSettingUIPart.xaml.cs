using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Services.Interfaces;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AreCloudServicesEnabledSettingUIPart.xaml
    /// </summary>
    public partial class IsCloudSyncEnabledSettingUIPart : SettingUIPart<bool>
    {
        private ICloudSynchronizationService _cloudSyncSvc;

        public IsCloudSyncEnabledSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
            Unloaded += IsCloudSyncEnabledSettingUIPart_Unloaded;
        }

        public bool IsNetworkAvailable
        {
            get { return (bool)GetValue(IsNetworkAvailableProperty); }
            set { SetValue(IsNetworkAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsNetworkAvailableProperty =
            DependencyProperty.Register(nameof(IsNetworkAvailable), typeof(bool), typeof(IsCloudSyncEnabledSettingUIPart), new PropertyMetadata(false));



        public bool IsAuthenticating
        {
            get { return (bool)GetValue(IsAuthenticatingProperty); }
            set { SetValue(IsAuthenticatingProperty, value); }
        }

        public static readonly DependencyProperty IsAuthenticatingProperty =
            DependencyProperty.Register(nameof(IsAuthenticating), typeof(bool), typeof(IsCloudSyncEnabledSettingUIPart), new PropertyMetadata(false));




        public bool IsAuthenticated
        {
            get { return (bool)GetValue(IsAuthenticatedProperty); }
            set { SetValue(IsAuthenticatedProperty, value); }
        }

        public static readonly DependencyProperty IsAuthenticatedProperty =
            DependencyProperty.Register(nameof(IsAuthenticated), typeof(bool), typeof(IsCloudSyncEnabledSettingUIPart), new PropertyMetadata(false));


        public bool IsInErrorState
        {
            get { return (bool)GetValue(IsInErrorStateProperty); }
            set { SetValue(IsInErrorStateProperty, value); }
        }

        public static readonly DependencyProperty IsInErrorStateProperty =
            DependencyProperty.Register(nameof(IsInErrorState), typeof(bool), typeof(IsCloudSyncEnabledSettingUIPart), new PropertyMetadata(false));

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(IsCloudSyncEnabledSettingUIPart), new PropertyMetadata(string.Empty));


        public bool IsCloudSyncEnabled
        {
            get { return (bool)GetValue(IsCloudSyncEnabledProperty); }
            set { SetValue(IsCloudSyncEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsCloudSyncEnabledProperty =
            DependencyProperty.Register(nameof(IsCloudSyncEnabled), typeof(bool), typeof(IsCloudSyncEnabledSettingUIPart), new PropertyMetadata(false, IsCloudSyncEnabled_PropertyChanged));

        private static void IsCloudSyncEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (IsCloudSyncEnabledSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsCloudSyncEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            IsCloudSyncEnabled = value;
        }

        public void SetCloudSyncService(ICloudSynchronizationService cloudSyncSvc)
        {
            _cloudSyncSvc = cloudSyncSvc;

            IsInErrorState = _cloudSyncSvc.IsInErrorState;
            ErrorMessage = _cloudSyncSvc.GetLastError(); 
            IsNetworkAvailable = _cloudSyncSvc.IsNetworkAvailable;
            IsAuthenticated = _cloudSyncSvc.IsAuthenticated;
            IsAuthenticating = _cloudSyncSvc.IsAuthenticating;

            _cloudSyncSvc.AuthenticationStateChanged += _cloudSyncSvc_AuthenticationStateChanged;
            _cloudSyncSvc.NetworkAvailabilityChanged += _cloudSyncSvc_NetworkAvailabilityChanged;
            _cloudSyncSvc.ErrorOccurred += _cloudSyncSvc_ErrorOccurred;
        }

        private void _cloudSyncSvc_ErrorOccurred(object sender, System.IO.ErrorEventArgs e)
        {
            IsInErrorState = _cloudSyncSvc.IsInErrorState;
            ErrorMessage = _cloudSyncSvc.GetLastError();
        }

        private void _cloudSyncSvc_NetworkAvailabilityChanged(object sender, System.EventArgs e)
        {
            IsNetworkAvailable = _cloudSyncSvc.IsNetworkAvailable;
        }

        private void _cloudSyncSvc_AuthenticationStateChanged(object sender, System.EventArgs e)
        {
            IsAuthenticated = _cloudSyncSvc.IsAuthenticated;
            IsAuthenticating = _cloudSyncSvc.IsAuthenticating;
        }

        private void IsCloudSyncEnabledSettingUIPart_Unloaded(object sender, RoutedEventArgs e)
        {
            _cloudSyncSvc.AuthenticationStateChanged -= _cloudSyncSvc_AuthenticationStateChanged;
            _cloudSyncSvc.NetworkAvailabilityChanged -= _cloudSyncSvc_NetworkAvailabilityChanged;
            _cloudSyncSvc.ErrorOccurred -= _cloudSyncSvc_ErrorOccurred;
        }
    }
}
