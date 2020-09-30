using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for NumOfRowsToPreloadSettingUIPart.xaml
    /// </summary>
    public partial class NumOfRowsToPreloadSettingUIPart : SettingUIPart<int>
    {
        public NumOfRowsToPreloadSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int NumOfRowsToPreload
        {
            get { return (int)GetValue(NumOfRowsToPreloadProperty); }
            set { SetValue(NumOfRowsToPreloadProperty, value); }
        }

        public static readonly DependencyProperty NumOfRowsToPreloadProperty =
            DependencyProperty.Register(nameof(NumOfRowsToPreload), typeof(int), typeof(NumOfRowsToPreloadSettingUIPart), new PropertyMetadata(10, NumOfRowsToPreload_PropertyChanged));

        private static void NumOfRowsToPreload_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (NumOfRowsToPreloadSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return NumOfRowsToPreload;
        }

        public override void SetSettingValue(int value)
        {
            NumOfRowsToPreload = value;
        }
    }
}
