using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardTrackingSettingUIPart.xaml
    /// </summary>
    public partial class ExternalSearchProcessSettingUIPart : SettingUIPart<bool>
    {
        public ExternalSearchProcessSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool ExternalSearchProcessEnabled
        {
            get { return (bool)GetValue(ExternalSearchProcessEnabledProperty); }
            set { SetValue(ExternalSearchProcessEnabledProperty, value); }
        }

        public static readonly DependencyProperty ExternalSearchProcessEnabledProperty =
            DependencyProperty.Register(nameof(ExternalSearchProcessEnabled), typeof(bool), typeof(ExternalSearchProcessSettingUIPart), new PropertyMetadata(false, ExternalSearchProcessEnabled_PropertyChanged));

        private static void ExternalSearchProcessEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ExternalSearchProcessSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return ExternalSearchProcessEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            ExternalSearchProcessEnabled = value;
        }
    }
}
