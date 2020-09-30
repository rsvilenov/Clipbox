using Clipbox.Infrastructure.UI;
using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;
using System.Windows.Input;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for MaxPreviewItemsInGroupSettingUIPart.xaml
    /// </summary>
    public partial class MaxPreviewItemsInGroupSettingUIPart : SettingUIPart<int>
    {
        public ICommand Increase { get; set; }

        public ICommand Decrease { get; set; }

        public MaxPreviewItemsInGroupSettingUIPart()
        {
            InitializeComponent();

            Increase = new DelegateCommand(() => slider.Value++, () => slider.Value < slider.Maximum);
            Decrease = new DelegateCommand(() => slider.Value--, () => slider.Value > slider.Minimum);

            this.mainGrid.DataContext = this;
        }

        public int MaxPreviewItemsInGroup
        {
            get { return (int)GetValue(MaxPreviewItemsInGroupProperty); }
            set { SetValue(MaxPreviewItemsInGroupProperty, value); }
        }

        public static readonly DependencyProperty MaxPreviewItemsInGroupProperty =
            DependencyProperty.Register(nameof(MaxPreviewItemsInGroup), typeof(int), typeof(MaxPreviewItemsInGroupSettingUIPart), new PropertyMetadata(10, MaxPreviewItemsInGroup_PropertyChanged));

        private static void MaxPreviewItemsInGroup_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (MaxPreviewItemsInGroupSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return MaxPreviewItemsInGroup;
        }

        public override void SetSettingValue(int value)
        {
            MaxPreviewItemsInGroup = value;
        }
    }
}
