using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for CompactifySettingUIPart.xaml
    /// </summary>
    public partial class CompactifySettingUIPart : SettingUIPart<bool>
    {
        public CompactifySettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsCompactifyToggled
        {
            get { return (bool)GetValue(IsCompactifyToggledProperty); }
            set { SetValue(IsCompactifyToggledProperty, value); }
        }

        public static readonly DependencyProperty IsCompactifyToggledProperty =
            DependencyProperty.Register(nameof(IsCompactifyToggled), typeof(bool), typeof(CompactifySettingUIPart), new PropertyMetadata(false, IsCompactifyToggled_PropertyChanged));

        private static void IsCompactifyToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (CompactifySettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsCompactifyToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsCompactifyToggled = value;
        }
    }
}
