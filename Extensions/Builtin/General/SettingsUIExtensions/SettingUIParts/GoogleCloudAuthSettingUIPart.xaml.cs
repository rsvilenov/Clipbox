using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.Extensions.BuiltIn.General.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for GoogleCloudAuthSettingUIPart.xaml
    /// </summary>
    public partial class GoogleCloudAuthSettingUIPart : SettingUIPart<bool>
    {
        public ICommand RequestReauth { get; set; }

        public GoogleCloudAuthSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;

            RequestReauth = new DelegateCommand(RequestReauthHandler);
        }

        private void RequestReauthHandler()
        {
            IsReauthRequested = true;
            NotifySettingModified();
        }

        public bool IsReauthRequested
        {
            get { return (bool)GetValue(IsReauthRequestedProperty); }
            set { SetValue(IsReauthRequestedProperty, value); }
        }

        public static readonly DependencyProperty IsReauthRequestedProperty =
            DependencyProperty.Register(nameof(IsReauthRequested), typeof(bool), typeof(GoogleCloudAuthSettingUIPart), new PropertyMetadata(false));


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
