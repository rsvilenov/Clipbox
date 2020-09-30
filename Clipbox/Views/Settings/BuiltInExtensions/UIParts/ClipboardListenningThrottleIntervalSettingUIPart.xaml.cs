using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Settings;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardListenningThrottleIntervalSettingUIPart.xaml
    /// </summary>
    public partial class ClipboardListenningThrottleIntervalSettingUIPart : SettingUIPart<int>
    {
        public ClipboardListenningThrottleIntervalSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int Interval
        {
            get { return (int)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(nameof(Interval), typeof(int), typeof(ClipboardListenningThrottleIntervalSettingUIPart), new PropertyMetadata(SettingsKeys.ClipboardListenningThrottleInterval.DefaultValue, Interval_PropertyChanged));

        private static void Interval_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ClipboardListenningThrottleIntervalSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return Interval;
        }

        public override void SetSettingValue(int value)
        {
            Interval = value;
        }
    }
}
