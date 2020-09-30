using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for StretchBackgroundSettingUIPart.xaml
    /// </summary>
    public partial class StretchBackgroundSettingUIPart : SettingUIPart<bool>
    {
        public StretchBackgroundSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsStretchToggled
        {
            get { return (bool)GetValue(IsStretchToggledProperty); }
            set { SetValue(IsStretchToggledProperty, value); }
        }

        public static readonly DependencyProperty IsStretchToggledProperty =
            DependencyProperty.Register(nameof(IsStretchToggled), typeof(bool), typeof(StretchBackgroundSettingUIPart), new PropertyMetadata(false, IsCompactifyToggled_PropertyChanged));

        private static void IsCompactifyToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (StretchBackgroundSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsStretchToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsStretchToggled = value;
        }
    }
}
