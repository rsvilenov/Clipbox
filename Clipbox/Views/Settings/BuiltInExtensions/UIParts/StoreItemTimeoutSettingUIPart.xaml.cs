using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for StoreItemTimeoutSettingUIPart.xaml
    /// </summary>
    public partial class StoreItemTimeoutSettingUIPart : SettingUIPart<int>
    {
        public StoreItemTimeoutSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int StoreItemTimeout
        {
            get { return (int)GetValue(StoreItemTimeoutProperty); }
            set { SetValue(StoreItemTimeoutProperty, value); }
        }

        public static readonly DependencyProperty StoreItemTimeoutProperty =
            DependencyProperty.Register(nameof(StoreItemTimeout), typeof(int), typeof(StoreItemTimeoutSettingUIPart), new PropertyMetadata(10, PreviewGenerationTimeout_PropertyChanged));

        private static void PreviewGenerationTimeout_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (StoreItemTimeoutSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return StoreItemTimeout;
        }

        public override void SetSettingValue(int value)
        {
            StoreItemTimeout = value;
        }
    }
}
