using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for PreserveFontPropsSettingUIPart.xaml
    /// </summary>
    public partial class SkipPasswordsSettingUIPart : SettingUIPart<bool>
    {
        public SkipPasswordsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsSkipPasswordsToggled
        {
            get { return (bool)GetValue(IsSkipPasswordsToggledProperty); }
            set { SetValue(IsSkipPasswordsToggledProperty, value); }
        }

        public static readonly DependencyProperty IsSkipPasswordsToggledProperty =
            DependencyProperty.Register(nameof(IsSkipPasswordsToggled), typeof(bool), typeof(SkipPasswordsSettingUIPart), new PropertyMetadata(false, IsSkipPasswordsToggled_PropertyChanged));

        private static void IsSkipPasswordsToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SkipPasswordsSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsSkipPasswordsToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsSkipPasswordsToggled = value;
        }
    }
}
