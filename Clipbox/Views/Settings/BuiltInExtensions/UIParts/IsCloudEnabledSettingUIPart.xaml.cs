using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for IsCloudEnabledSettingUIPart.xaml
    /// </summary>
    public partial class IsCloudEnabledSettingUIPart : SettingUIPart<bool>
    {
        public IsCloudEnabledSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsCloudEnabled
        {
            get { return (bool)GetValue(IsCloudEnabledProperty); }
            set { SetValue(IsCloudEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsCloudEnabledProperty =
            DependencyProperty.Register(nameof(IsCloudEnabled), typeof(bool), typeof(IsCloudEnabledSettingUIPart), new PropertyMetadata(false, IsCloudEnabled_PropertyChanged));

        private static void IsCloudEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (IsCloudEnabledSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsCloudEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            IsCloudEnabled = value;
            tglSwitch.SetSwitchState(value);
        }
    }
}
