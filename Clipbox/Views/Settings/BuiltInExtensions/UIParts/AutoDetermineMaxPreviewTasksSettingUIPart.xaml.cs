using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for AutoDetermineMaxPreviewTasksSettingUIPart.xaml
    /// </summary>
    public partial class AutoDetermineMaxPreviewTasksSettingUIPart : SettingUIPart<bool>
    {
        public AutoDetermineMaxPreviewTasksSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsAutoDetermineMaxPreviewTasksEnabled
        {
            get { return (bool)GetValue(IsAutoDetermineMaxPreviewTasksEnabledProperty); }
            set { SetValue(IsAutoDetermineMaxPreviewTasksEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAutoDetermineMaxPreviewTasksEnabledProperty =
            DependencyProperty.Register(nameof(IsAutoDetermineMaxPreviewTasksEnabled), typeof(bool), typeof(AutoDetermineMaxPreviewTasksSettingUIPart), new PropertyMetadata(false, IsAutoDetermineMaxPreviewTasksEnabled_PropertyChanged));

        private static void IsAutoDetermineMaxPreviewTasksEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (AutoDetermineMaxPreviewTasksSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsAutoDetermineMaxPreviewTasksEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            IsAutoDetermineMaxPreviewTasksEnabled = value;
        }
    }
}
