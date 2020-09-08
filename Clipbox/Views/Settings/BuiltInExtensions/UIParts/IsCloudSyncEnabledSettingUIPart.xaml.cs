using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for IsCloudSyncEnabledSettingUIPart.xaml
    /// </summary>
    public partial class IsCloudSyncEnabledSettingUIPart : SettingUIPart<bool>
    {
        public IsCloudSyncEnabledSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

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
    }
}
