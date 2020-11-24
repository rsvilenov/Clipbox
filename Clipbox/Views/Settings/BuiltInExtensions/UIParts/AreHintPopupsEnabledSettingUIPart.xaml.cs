using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AreHintPopupsEnabledSettingUIPart.xaml
    /// </summary>
    public partial class AreHintPopupsEnabledSettingUIPart : SettingUIPart<bool>
    {
        public AreHintPopupsEnabledSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool AreHintTooltipsEnabled
        {
            get { return (bool)GetValue(AreHintTooltipsEnabledProperty); }
            set { SetValue(AreHintTooltipsEnabledProperty, value); }
        }

        public static readonly DependencyProperty AreHintTooltipsEnabledProperty =
            DependencyProperty.Register(nameof(AreHintTooltipsEnabled), typeof(bool), typeof(AreHintPopupsEnabledSettingUIPart), new PropertyMetadata(false, AreHintTooltipsEnabled_PropertyChanged));

        private static void AreHintTooltipsEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AreHintPopupsEnabledSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return AreHintTooltipsEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            AreHintTooltipsEnabled = value;
        }
    }
}
