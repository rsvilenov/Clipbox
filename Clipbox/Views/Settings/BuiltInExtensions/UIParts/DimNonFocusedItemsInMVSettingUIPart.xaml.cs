using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Views.Settings.BuiltInExtensions.UIParts
{
    /// <summary>
    /// Interaction logic for DimNonFocusedItemsInQVSettingUIPart.xaml
    /// </summary>
    public partial class DimNonFocusedItemsInMVSettingUIPart : SettingUIPart<bool>
    {
        public DimNonFocusedItemsInMVSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool DimNonFocusedItemsInMV
        {
            get { return (bool)GetValue(DimNonFocusedItemsInMVProperty); }
            set { SetValue(DimNonFocusedItemsInMVProperty, value); }
        }

        public static readonly DependencyProperty DimNonFocusedItemsInMVProperty =
            DependencyProperty.Register(nameof(DimNonFocusedItemsInMV), typeof(bool), typeof(DimNonFocusedItemsInMVSettingUIPart), new PropertyMetadata(false, DimNonFocusedItemsInMV_PropertyChanged));

        private static void DimNonFocusedItemsInMV_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DimNonFocusedItemsInMVSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return DimNonFocusedItemsInMV;
        }

        public override void SetSettingValue(bool value)
        {
            DimNonFocusedItemsInMV = value;
        }
    }
}
