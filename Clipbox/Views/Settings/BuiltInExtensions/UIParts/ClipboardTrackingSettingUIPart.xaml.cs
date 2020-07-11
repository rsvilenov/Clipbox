using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardTrackingSettingUIPart.xaml
    /// </summary>
    public partial class ClipboardTrackingSettingUIPart : SettingUIPart<bool>
    {
        public ClipboardTrackingSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsClipboardTrackingEnabled
        {
            get { return (bool)GetValue(IsClipboardTrackingEnabledProperty); }
            set { SetValue(IsClipboardTrackingEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsClipboardTrackingEnabledProperty =
            DependencyProperty.Register(nameof(IsClipboardTrackingEnabled), typeof(bool), typeof(ClipboardTrackingSettingUIPart), new PropertyMetadata(false, IsClipboardTrackingEnabled_PropertyChanged));

        private static void IsClipboardTrackingEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ClipboardTrackingSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsClipboardTrackingEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            IsClipboardTrackingEnabled = value;
        }
    }
}
