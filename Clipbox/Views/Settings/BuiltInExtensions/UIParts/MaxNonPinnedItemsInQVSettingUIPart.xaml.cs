using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for MaxNonPinnedItemsInQVSettingUIPart.xaml
    /// </summary>
    public partial class MaxNonPinnedItemsInQVSettingUIPart : SettingUIPart<int>
    {
        public ICommand Increase { get; set; }

        public ICommand Decrease { get; set; }

        public MaxNonPinnedItemsInQVSettingUIPart()
        {
            InitializeComponent();

            Increase = new DelegateCommand(() => slider.Value++, () => slider.Value < slider.Maximum);
            Decrease = new DelegateCommand(() => slider.Value--, () => slider.Value > slider.Minimum);

            this.mainGrid.DataContext = this;
        }

        public int MaxNonPinnedPinnedItemsInQV
        {
            get { return (int)GetValue(MaxNonPinnedPinnedItemsInQVProperty); }
            set { SetValue(MaxNonPinnedPinnedItemsInQVProperty, value); }
        }

        public static readonly DependencyProperty MaxNonPinnedPinnedItemsInQVProperty =
            DependencyProperty.Register(nameof(MaxNonPinnedPinnedItemsInQV), typeof(int), typeof(MaxNonPinnedItemsInQVSettingUIPart), new PropertyMetadata(10, MaxNonPinnedPinnedItemsInQV_PropertyChanged));

        private static void MaxNonPinnedPinnedItemsInQV_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MaxNonPinnedItemsInQVSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return MaxNonPinnedPinnedItemsInQV;
        }

        public override void SetSettingValue(int value)
        {
            MaxNonPinnedPinnedItemsInQV = value;
        }
    }
}
