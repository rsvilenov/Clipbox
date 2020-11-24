using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AreBaloonPopupsEnabledSettingUIPart.xaml
    /// </summary>
    public partial class AreBaloonPopupsEnabledSettingUIPart : SettingUIPart<bool>
    {
        public AreBaloonPopupsEnabledSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool AreBaloonTooltipsEnabled
        {
            get { return (bool)GetValue(AreBaloonTooltipsEnabledProperty); }
            set { SetValue(AreBaloonTooltipsEnabledProperty, value); }
        }

        public static readonly DependencyProperty AreBaloonTooltipsEnabledProperty =
            DependencyProperty.Register(nameof(AreBaloonTooltipsEnabled), typeof(bool), typeof(AreBaloonPopupsEnabledSettingUIPart), new PropertyMetadata(false, AreBaloonTooltipsEnabled_PropertyChanged));

        private static void AreBaloonTooltipsEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AreBaloonPopupsEnabledSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return AreBaloonTooltipsEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            AreBaloonTooltipsEnabled = value;
        }
    }
}
