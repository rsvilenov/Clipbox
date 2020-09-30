using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Settings;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardFormatSupportExtTimeoutSettingUIPart.xaml
    /// </summary>
    public partial class ClipboardFormatSupportExtTimeoutSettingUIPart : SettingUIPart<int>
    {
        public ClipboardFormatSupportExtTimeoutSettingUIPart()
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
            DependencyProperty.Register(nameof(Timeout), typeof(int), typeof(ClipboardFormatSupportExtTimeoutSettingUIPart), new PropertyMetadata(SettingsKeys.ClipboardFormatSupportExtTimeout.DefaultValue, Timeout_PropertyChanged));

        private static void Timeout_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ClipboardFormatSupportExtTimeoutSettingUIPart)d;
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
