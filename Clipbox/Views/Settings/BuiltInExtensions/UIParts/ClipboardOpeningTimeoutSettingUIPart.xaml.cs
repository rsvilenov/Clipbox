using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Settings;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardOpeningTimeoutSettingUIPart.xaml
    /// </summary>
    public partial class ClipboardOpeningTimeoutSettingUIPart : SettingUIPart<int>
    {
        public ClipboardOpeningTimeoutSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int Timeout
        {
            get { return (int)GetValue(TimeoutProperty); }
            set { SetValue(TimeoutProperty, value); }
        }

        public static readonly DependencyProperty TimeoutProperty =
            DependencyProperty.Register(nameof(Timeout), typeof(int), typeof(ClipboardOpeningTimeoutSettingUIPart), new PropertyMetadata(SettingsKeys.ClipboardOpenningTimeoutMs.DefaultValue, Timeout_PropertyChanged));

        private static void Timeout_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ClipboardOpeningTimeoutSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return Timeout;
        }

        public override void SetSettingValue(int value)
        {
            Timeout = value;
        }
    }
}
