using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for MaxPinnedItemsSettingUIPart.xaml
    /// </summary>
    public partial class MaxPinnedItemsSettingUIPart : SettingUIPart<int>
    {
        public ICommand Increase { get; set; }

        public ICommand Decrease { get; set; }

        public MaxPinnedItemsSettingUIPart()
        {
            InitializeComponent();

            Increase = new DelegateCommand(() => slider.Value++, () => slider.Value < slider.Maximum);
            Decrease = new DelegateCommand(() => slider.Value--, () => slider.Value > slider.Minimum);

            this.mainGrid.DataContext = this;
        }

        public int MaxPinnedItems
        {
            get { return (int)GetValue(MaxPinnedItemsProperty); }
            set { SetValue(MaxPinnedItemsProperty, value); }
        }

        public static readonly DependencyProperty MaxPinnedItemsProperty =
            DependencyProperty.Register(nameof(MaxPinnedItems), typeof(int), typeof(MaxPinnedItemsSettingUIPart), new PropertyMetadata(10, MaxPinnedItems_PropertyChanged));

        private static void MaxPinnedItems_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MaxPinnedItemsSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return MaxPinnedItems;
        }

        public override void SetSettingValue(int value)
        {
            MaxPinnedItems = value;
        }
    }
}
