using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardTrackingSettingUIPart.xaml
    /// </summary>
    public partial class FadeAnimationsSettingUIPart : SettingUIPart<bool>
    {
        public FadeAnimationsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool AreFadeAnimationsEnabled
        {
            get { return (bool)GetValue(AreFadeAnimationsEnabledProperty); }
            set { SetValue(AreFadeAnimationsEnabledProperty, value); }
        }

        public static readonly DependencyProperty AreFadeAnimationsEnabledProperty =
            DependencyProperty.Register(nameof(AreFadeAnimationsEnabled), typeof(bool), typeof(FadeAnimationsSettingUIPart), new PropertyMetadata(false, AreFadeAnimationsEnabled_PropertyChanged));

        private static void AreFadeAnimationsEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (FadeAnimationsSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return AreFadeAnimationsEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            AreFadeAnimationsEnabled = value;
        }
    }
}
