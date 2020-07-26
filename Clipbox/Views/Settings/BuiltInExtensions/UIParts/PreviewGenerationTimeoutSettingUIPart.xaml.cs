using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for PreviewGenerationTimeoutSettingUIPart.xaml
    /// </summary>
    public partial class PreviewGenerationTimeoutSettingUIPart : SettingUIPart<int>
    {
        public PreviewGenerationTimeoutSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int PreviewGenerationTimeout
        {
            get { return (int)GetValue(PreviewGenerationTimeoutProperty); }
            set { SetValue(PreviewGenerationTimeoutProperty, value); }
        }

        public static readonly DependencyProperty PreviewGenerationTimeoutProperty =
            DependencyProperty.Register(nameof(PreviewGenerationTimeout), typeof(int), typeof(PreviewGenerationTimeoutSettingUIPart), new PropertyMetadata(10, PreviewGenerationTimeout_PropertyChanged));

        private static void PreviewGenerationTimeout_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PreviewGenerationTimeoutSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return PreviewGenerationTimeout;
        }

        public override void SetSettingValue(int value)
        {
            PreviewGenerationTimeout = value;
        }
    }
}
