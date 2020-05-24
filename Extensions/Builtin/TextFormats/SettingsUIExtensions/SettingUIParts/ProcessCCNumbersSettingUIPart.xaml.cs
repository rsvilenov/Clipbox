using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for ProcessCCNumbersSettingUIPart.xaml
    /// </summary>
    public partial class ProcessCCNumbersSettingUIPart : SettingUIPart<bool>
    {
        public ProcessCCNumbersSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsProcessCCNumbersToggled
        {
            get { return (bool)GetValue(IsProcessCCNumbersToggledProperty); }
            set { SetValue(IsProcessCCNumbersToggledProperty, value); }
        }

        public static readonly DependencyProperty IsProcessCCNumbersToggledProperty =
            DependencyProperty.Register(nameof(IsProcessCCNumbersToggled), typeof(bool), typeof(ProcessCCNumbersSettingUIPart), new PropertyMetadata(false, IsProcessCCNumbersToggled_PropertyChanged));

        private static void IsProcessCCNumbersToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (ProcessCCNumbersSettingUIPart)d;
            
            self.NotifySettingModified();
        }
        
        public override bool GetSettingValue()
        {
            return IsProcessCCNumbersToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsProcessCCNumbersToggled = value;
        }
    }
}
