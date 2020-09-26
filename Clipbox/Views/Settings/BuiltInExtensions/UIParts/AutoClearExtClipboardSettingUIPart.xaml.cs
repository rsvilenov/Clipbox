using Clipbox.Infrastructure.UI.Extensibility;
using Clipbox.Settings;
using System;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoClearExtClipboardSettingUIPart.xaml
    /// </summary>
    public partial class AutoClearExtClipboardSettingUIPart : SettingUIPart<int>
    {
        public AutoClearExtClipboardSettingUIPart()
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
            DependencyProperty.Register(nameof(IsAutoClearEnabled), typeof(bool), typeof(AutoClearExtClipboardSettingUIPart), new PropertyMetadata(false, IsAutoClearEnabled_PropertyChanged));

        private static void IsAutoClearEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoClearExtClipboardSettingUIPart)d; 
            self.AutoClearExtClipboardAfterSeconds = (bool)e.NewValue 
                ? Math.Abs(self.AutoClearExtClipboardAfterSeconds) :
                 Math.Abs(self.AutoClearExtClipboardAfterSeconds) * -1;
        }

        public int AutoClearExtClipboardAfterSeconds
        {
            get { return (int)GetValue(AutoClearExtClipboardAfterSecondsProperty); }
            set { SetValue(AutoClearExtClipboardAfterSecondsProperty, value); }
        }

        public static readonly DependencyProperty AutoClearExtClipboardAfterSecondsProperty =
            DependencyProperty.Register(nameof(AutoClearExtClipboardAfterSeconds), typeof(int), typeof(AutoClearExtClipboardSettingUIPart), new PropertyMetadata(0, AutoClearExtClipboardAfterSeconds_PropertyChanged));

        private static void AutoClearExtClipboardAfterSeconds_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoClearExtClipboardSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return AutoClearExtClipboardAfterSeconds;
        }

        public override void SetSettingValue(int value)
        {
            if (value == 0)
            {
                value = SettingsKeys.AutoClearExtClipboardAfterSeconds.DefaultValue;
            }

            AutoClearExtClipboardAfterSeconds = value;
            IsAutoClearEnabled = value > 0;
        }
    }
}