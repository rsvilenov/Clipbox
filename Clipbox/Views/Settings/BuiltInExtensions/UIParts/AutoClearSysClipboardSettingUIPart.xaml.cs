using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Settings;
using System;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoClearSysClipboardSettingUIPart.xaml
    /// </summary>
    public partial class AutoClearSysClipboardSettingUIPart : SettingUIPart<int>
    {
        public AutoClearSysClipboardSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsAutoClearEnabled
        {
            get { return (bool)GetValue(IsAutoClearEnabledProperty); }
            set { SetValue(IsAutoClearEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAutoClearEnabledProperty =
            DependencyProperty.Register(nameof(IsAutoClearEnabled), typeof(bool), typeof(AutoClearSysClipboardSettingUIPart), new PropertyMetadata(false, IsAutoClearEnabled_PropertyChanged));

        private static void IsAutoClearEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoClearSysClipboardSettingUIPart)d;
            self.AutoClearSysClipboardAfterSeconds = (bool)e.NewValue
                ? Math.Abs(self.AutoClearSysClipboardAfterSeconds) :
                 Math.Abs(self.AutoClearSysClipboardAfterSeconds) * -1;
        }

        public int AutoClearSysClipboardAfterSeconds
        {
            get { return (int)GetValue(AutoClearSysClipboardAfterSecondsProperty); }
            set { SetValue(AutoClearSysClipboardAfterSecondsProperty, value); }
        }

        public static readonly DependencyProperty AutoClearSysClipboardAfterSecondsProperty =
            DependencyProperty.Register(nameof(AutoClearSysClipboardAfterSeconds), typeof(int), typeof(AutoClearSysClipboardSettingUIPart), new PropertyMetadata(0, AutoClearSysClipboardAfterSeconds_PropertyChanged));

        private static void AutoClearSysClipboardAfterSeconds_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoClearSysClipboardSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return AutoClearSysClipboardAfterSeconds;
        }

        public override void SetSettingValue(int value)
        {
            if (value == 0)
            {
                value = SettingsKeys.AutoClearSysClipboardAfterSeconds.DefaultValue;
            }

            AutoClearSysClipboardAfterSeconds =value;
            IsAutoClearEnabled = value > 0;
        }
    }
}
