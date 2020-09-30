using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for MainProcessPrioritySettingUIPart.xaml
    /// </summary>
    public partial class MainProcessPrioritySettingUIPart : SettingUIPart<bool>
    {
        public MainProcessPrioritySettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool MainProcessHighPriorityEnabled
        {
            get { return (bool)GetValue(MainProcessHighPriorityEnabledProperty); }
            set { SetValue(MainProcessHighPriorityEnabledProperty, value); }
        }

        public static readonly DependencyProperty MainProcessHighPriorityEnabledProperty =
            DependencyProperty.Register(nameof(MainProcessHighPriorityEnabled), typeof(bool), typeof(MainProcessPrioritySettingUIPart), new PropertyMetadata(false, MainProcessHighPriorityEnabled_PropertyChanged));

        private static void MainProcessHighPriorityEnabled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MainProcessPrioritySettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return MainProcessHighPriorityEnabled;
        }

        public override void SetSettingValue(bool value)
        {
            MainProcessHighPriorityEnabled = value;
        }
    }
}
