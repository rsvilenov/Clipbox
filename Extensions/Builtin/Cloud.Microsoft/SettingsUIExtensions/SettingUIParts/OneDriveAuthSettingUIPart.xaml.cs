using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.Extensions.BuiltIn.Cloud.Microsoft.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for OneDriveAuthSettingUIPart.xaml
    /// </summary>
    public partial class OneDriveAuthSettingUIPart : SettingUIPart<bool>
    {
        public ICommand RequestReauth { get; set; }

        public OneDriveAuthSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            RequestReauth = new DelegateCommand(RequestReauthHandler);
        }

        private void RequestReauthHandler()
        {
            IsReauthRequested = !IsReauthRequested;
            NotifySettingModified();
        }

        public bool IsReauthRequested
        {
            get { return (bool)GetValue(IsReauthRequestedProperty); }
            set { SetValue(IsReauthRequestedProperty, value); }
        }

        public static readonly DependencyProperty IsReauthRequestedProperty =
            DependencyProperty.Register(nameof(IsReauthRequested), typeof(bool), typeof(OneDriveAuthSettingUIPart), new PropertyMetadata(false));


        public override bool GetSettingValue()
        {
            return IsReauthRequested;
        }

        public override void SetSettingValue(bool value)
        {
            IsReauthRequested = value;
        }
    }
}
