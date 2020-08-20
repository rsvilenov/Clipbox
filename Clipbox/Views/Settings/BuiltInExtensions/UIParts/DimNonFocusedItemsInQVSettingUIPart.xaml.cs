using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for DimNonFocusedItemsInQVSettingUIPart.xaml
    /// </summary>
    public partial class DimNonFocusedItemsInQVSettingUIPart : SettingUIPart<bool>
    {
        public DimNonFocusedItemsInQVSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool DimNonFocusedItemsInQV
        {
            get { return (bool)GetValue(DimNonFocusedItemsInQVProperty); }
            set { SetValue(DimNonFocusedItemsInQVProperty, value); }
        }

        public static readonly DependencyProperty DimNonFocusedItemsInQVProperty =
            DependencyProperty.Register(nameof(DimNonFocusedItemsInQV), typeof(bool), typeof(DimNonFocusedItemsInQVSettingUIPart), new PropertyMetadata(false, DimNonFocusedItemsInQV_PropertyChanged));

        private static void DimNonFocusedItemsInQV_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DimNonFocusedItemsInQVSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return DimNonFocusedItemsInQV;
        }

        public override void SetSettingValue(bool value)
        {
            DimNonFocusedItemsInQV = value;
        }
    }
}
