using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for PinTaskbarIconSettingUIPart.xaml
    /// </summary>
    public partial class ScreenHightItemHeightRatioSettingUIPart : SettingUIPart<double>
    {
        public ICommand Increase { get; set; }

        public ICommand Decrease { get; set; }

        public ScreenHightItemHeightRatioSettingUIPart()
        {
            InitializeComponent();

            Increase = new DelegateCommand(() => slider.Value += slider.TickFrequency, () => slider.Value < slider.Maximum);
            Decrease = new DelegateCommand(() => slider.Value -= slider.TickFrequency, () => slider.Value > slider.Minimum);

            this.mainGrid.DataContext = this;
        }

        public double ItemHeightRatio
        {
            get { return (double)GetValue(ItemHeightRatioProperty); }
            set { SetValue(ItemHeightRatioProperty, value); }
        }

        public static readonly DependencyProperty ItemHeightRatioProperty =
            DependencyProperty.Register(nameof(ItemHeightRatio), typeof(double), typeof(ScreenHightItemHeightRatioSettingUIPart), new PropertyMetadata(0.13d, ItemHeightRatio_PropertyChanged));

        private static void ItemHeightRatio_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ScreenHightItemHeightRatioSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override double GetSettingValue()
        {
            return ItemHeightRatio;
        }

        public override void SetSettingValue(double value)
        {
            ItemHeightRatio = value;
        }
    }
}
