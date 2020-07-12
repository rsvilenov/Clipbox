using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for ClipboardTrackingSettingUIPart.xaml
    /// </summary>
    public partial class BackgroundPreviewGenerationSettingUIPart : SettingUIPart<bool>
    {
        public BackgroundPreviewGenerationSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsBackgroundPreviewGenerationEnabled
        {
            get { return (bool)GetValue(IsBackgroundPreviewGenerationEnabledProperty); }
            set { SetValue(IsBackgroundPreviewGenerationEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsBackgroundPreviewGenerationEnabledProperty =
            DependencyProperty.Register(nameof(IsBackgroundPreviewGenerationEnabled), typeof(bool), typeof(BackgroundPreviewGenerationSettingUIPart), new PropertyMetadata(false, IsBackgroundPreviewGenerationEnabled_PropertyChanged));

        private static void IsBackgroundPreviewGenerationEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (BackgroundPreviewGenerationSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsBackgroundPreviewGenerationEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            IsBackgroundPreviewGenerationEnabled = value;
        }
    }
}
