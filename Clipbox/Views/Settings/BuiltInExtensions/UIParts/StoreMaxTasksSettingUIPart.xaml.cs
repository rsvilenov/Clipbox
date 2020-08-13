using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for StoreMaxTasksSettingUIPart.xaml
    /// </summary>
    public partial class StoreMaxTasksSettingUIPart : SettingUIPart<int>
    {
        public StoreMaxTasksSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int StoreMaxTasksTimeout
        {
            get { return (int)GetValue(StoreMaxTasksProperty); }
            set { SetValue(StoreMaxTasksProperty, value); }
        }

        public static readonly DependencyProperty StoreMaxTasksProperty =
            DependencyProperty.Register(nameof(StoreMaxTasksTimeout), typeof(int), typeof(StoreMaxTasksSettingUIPart), new PropertyMetadata(10, StoreMaxTasksTimeout_PropertyChanged));

        private static void StoreMaxTasksTimeout_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (StoreMaxTasksSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return StoreMaxTasksTimeout;
        }

        public override void SetSettingValue(int value)
        {
            StoreMaxTasksTimeout = value;
        }
    }
}
