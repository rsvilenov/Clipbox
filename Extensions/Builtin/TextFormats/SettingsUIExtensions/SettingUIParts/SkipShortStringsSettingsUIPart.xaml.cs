using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for SkipShortStringsSettingsUIPart.xaml
    /// </summary>
    public partial class SkipShortStringsSettingsUIPart : SettingUIPart<int>
    {
        const int DEFAULT_MIN_TEXT_LEN = 10;

        public SkipShortStringsSettingsUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsShipShortStringsToggled
        {
            get { return (bool)GetValue(IsShipShortStringsToggledProperty); }
            set { SetValue(IsShipShortStringsToggledProperty, value); }
        }

        public static readonly DependencyProperty IsShipShortStringsToggledProperty =
            DependencyProperty.Register(nameof(IsShipShortStringsToggled), typeof(bool), typeof(SkipShortStringsSettingsUIPart), new PropertyMetadata(false, IsShipShortStringsToggled_PropertyChanged));

        private static void IsShipShortStringsToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SkipShortStringsSettingsUIPart)d;

            if ((bool)e.NewValue && self.MinTextLength == 0)
            {
                self.MinTextLength = DEFAULT_MIN_TEXT_LEN;
            }

            self.NotifySettingModified();
        }
        
        public int? MinTextLength
        {
            get { return (int?)GetValue(MinTextLengthProperty); }
            set { SetValue(MinTextLengthProperty, value); }
        }

        public static readonly DependencyProperty MinTextLengthProperty =
            DependencyProperty.Register(nameof(MinTextLength), typeof(int?), typeof(SkipShortStringsSettingsUIPart), new PropertyMetadata(0, MinTextLength_PropertyChanged));

        private static void MinTextLength_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SkipShortStringsSettingsUIPart)d;
            self.NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return IsShipShortStringsToggled ? MinTextLength ?? 0 : 0;
        }

        public override void SetSettingValue(int value)
        {
            if (value == 0)
            {
                IsShipShortStringsToggled = false;
                MinTextLength = DEFAULT_MIN_TEXT_LEN;
            }
            else
            {
                IsShipShortStringsToggled = true;
                MinTextLength = value;
            }
        }
    }
}
