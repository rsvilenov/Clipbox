using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for CloudAuthTimeoutSettingUIPart.xaml
    /// </summary>
    public partial class CloudAuthTimeoutSettingUIPart : SettingUIPart<int>
    {
        public CloudAuthTimeoutSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int CloudServiceAuthAttemptTimeout
        {
            get { return (int)GetValue(PreviewGenerationTimeoutProperty); }
            set { SetValue(PreviewGenerationTimeoutProperty, value); }
        }

        public static readonly DependencyProperty PreviewGenerationTimeoutProperty =
            DependencyProperty.Register(nameof(CloudServiceAuthAttemptTimeout), typeof(int), typeof(CloudAuthTimeoutSettingUIPart), new PropertyMetadata(20000, CloudServiceAuthAttemptTimeout_PropertyChanged));

        private static void CloudServiceAuthAttemptTimeout_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (CloudAuthTimeoutSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return CloudServiceAuthAttemptTimeout;
        }

        public override void SetSettingValue(int value)
        {
            CloudServiceAuthAttemptTimeout = value;
        }
    }
}
