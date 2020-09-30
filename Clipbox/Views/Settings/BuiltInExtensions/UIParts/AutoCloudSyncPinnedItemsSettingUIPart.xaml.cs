using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoCloudSyncPinnedItemsSettingUIPart.xaml
    /// </summary>
    public partial class AutoCloudSyncPinnedItemsSettingUIPart : SettingUIPart<bool>
    {
        public AutoCloudSyncPinnedItemsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsAutomaticCloudSyncOfPinnedItemsEnabled
        {
            get { return (bool)GetValue(IsAutomaticCloudSyncOfPinnedItemsProperty); }
            set { SetValue(IsAutomaticCloudSyncOfPinnedItemsProperty, value); }
        }

        public static readonly DependencyProperty IsAutomaticCloudSyncOfPinnedItemsProperty =
            DependencyProperty.Register(nameof(IsAutomaticCloudSyncOfPinnedItemsEnabled), typeof(bool), typeof(BackgroundPreviewGenerationSettingUIPart), new PropertyMetadata(false, IsAutomaticCloudSyncOfPinnedItemsEnabled_PropertyChanged));

        private static void IsAutomaticCloudSyncOfPinnedItemsEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoCloudSyncPinnedItemsSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsAutomaticCloudSyncOfPinnedItemsEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            IsAutomaticCloudSyncOfPinnedItemsEnabled = value;
        }
    }
}
