using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoClearExtClipboardUponLockScreenSettingUIPart.xaml
    /// </summary>
    public partial class AutoClearExtClipboardUponLockScreenSettingUIPart : SettingUIPart<bool>
    {
        public AutoClearExtClipboardUponLockScreenSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool ClearUponLockScreenEnabled
        {
            get { return (bool)GetValue(ClearUponLockScreenEnabledProperty); }
            set { SetValue(ClearUponLockScreenEnabledProperty, value); }
        }

        public static readonly DependencyProperty ClearUponLockScreenEnabledProperty =
            DependencyProperty.Register(nameof(ClearUponLockScreenEnabled), typeof(bool), typeof(AutoClearExtClipboardUponLockScreenSettingUIPart), new PropertyMetadata(false, ClearUponLogoffEnabled_PropertyChanged));

        private static void ClearUponLogoffEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoClearExtClipboardUponLockScreenSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return ClearUponLockScreenEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            ClearUponLockScreenEnabled = value;
        }
    }
}
