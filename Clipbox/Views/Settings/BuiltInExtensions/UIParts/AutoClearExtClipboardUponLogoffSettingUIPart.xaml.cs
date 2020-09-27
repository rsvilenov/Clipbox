using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoClearExtClipboardUponLogoffSettingUIPart.xaml
    /// </summary>
    public partial class AutoClearExtClipboardUponLogoffSettingUIPart : SettingUIPart<bool>
    {
        public AutoClearExtClipboardUponLogoffSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool ClearUponLogoffEnabled
        {
            get { return (bool)GetValue(ClearUponLogoffEnabledProperty); }
            set { SetValue(ClearUponLogoffEnabledProperty, value); }
        }

        public static readonly DependencyProperty ClearUponLogoffEnabledProperty =
            DependencyProperty.Register(nameof(ClearUponLogoffEnabled), typeof(bool), typeof(AutoClearExtClipboardUponLogoffSettingUIPart), new PropertyMetadata(false, ClearUponLogoffEnabled_PropertyChanged));

        private static void ClearUponLogoffEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoClearExtClipboardUponLogoffSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return ClearUponLogoffEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            ClearUponLogoffEnabled = value;
        }
    }
}
