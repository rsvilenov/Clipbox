using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AreCloudServicesEnabledSettingUIPart.xaml
    /// </summary>
    public partial class AreCloudServicesEnabledSettingUIPart : SettingUIPart<bool>
    {
        public AreCloudServicesEnabledSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool AreCloudSvcsEnabled
        {
            get { return (bool)GetValue(AreCloudSvcsEnabledProperty); }
            set { SetValue(AreCloudSvcsEnabledProperty, value); }
        }

        public static readonly DependencyProperty AreCloudSvcsEnabledProperty =
            DependencyProperty.Register(nameof(AreCloudSvcsEnabled), typeof(bool), typeof(AreCloudServicesEnabledSettingUIPart), new PropertyMetadata(false, AreCloudSvcsEnabled_PropertyChanged));

        private static void AreCloudSvcsEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AreCloudServicesEnabledSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return AreCloudSvcsEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            AreCloudSvcsEnabled = value;
        }
    }
}
