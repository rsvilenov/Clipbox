using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for NumItemsOnPageSettingUIPart.xaml
    /// </summary>
    public partial class NumItemsOnPageSettingUIPart : SettingUIPart<int>
    {
        public NumItemsOnPageSettingUIPart()
        {
            InitializeComponent();

            this.mainGrid.DataContext = this;
        }

        public int NumItemsOnPage
        {
            get { return (int)GetValue(NumItemsOnPageProperty); }
            set { SetValue(NumItemsOnPageProperty, value); }
        }

        public static readonly DependencyProperty NumItemsOnPageProperty =
            DependencyProperty.Register(nameof(NumItemsOnPage), typeof(int), typeof(NumItemsOnPageSettingUIPart), new PropertyMetadata(10, NumItemsOnPage_PropertyChanged));

        private static void NumItemsOnPage_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (NumItemsOnPageSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return NumItemsOnPage;
        }

        public override void SetSettingValue(int value)
        {
            NumItemsOnPage = value;
        }
    }
}
