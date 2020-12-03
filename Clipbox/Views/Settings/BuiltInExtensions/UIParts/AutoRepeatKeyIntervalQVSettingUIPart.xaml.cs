using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Settings;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoRepeatKeyIntervalQVSettingUIPart.xaml
    /// </summary>
    public partial class AutoRepeatKeyIntervalQVSettingUIPart : SettingUIPart<int>
    {
        public AutoRepeatKeyIntervalQVSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int KeyRepeatInterval
        {
            get { return (int)GetValue(KeyRepeatIntervalProperty); }
            set { SetValue(KeyRepeatIntervalProperty, value); }
        }

        public static readonly DependencyProperty KeyRepeatIntervalProperty =
            DependencyProperty.Register(nameof(KeyRepeatInterval), typeof(int), typeof(AutoRepeatKeyIntervalQVSettingUIPart), new PropertyMetadata(SettingsKeys.ClipboardFormatSupportExtTimeout.DefaultValue, Timeout_KeyRepeatInterval));

        private static void Timeout_KeyRepeatInterval(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoRepeatKeyIntervalQVSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return KeyRepeatInterval;
        }

        public override void SetSettingValue(int value)
        {
            KeyRepeatInterval = value;
        }
    }
}
