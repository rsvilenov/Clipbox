using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for PreserveFontPropsSettingUIPart.xaml
    /// </summary>
    public partial class PreserveFontPropsSettingUIPart : SettingUIPart<bool>
    {
        public PreserveFontPropsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }
        
        public bool IsPreserveFontPropsToggled
        {
            get { return (bool)GetValue(IsPreserveFontPropsToggledProperty); }
            set { SetValue(IsPreserveFontPropsToggledProperty, value); }
        }

        public static readonly DependencyProperty IsPreserveFontPropsToggledProperty =
            DependencyProperty.Register(nameof(IsPreserveFontPropsToggled), typeof(bool), typeof(PreserveFontPropsSettingUIPart), new PropertyMetadata(false, IsPreserveFontPropsToggled_PropertyChanged));

        private static void IsPreserveFontPropsToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PreserveFontPropsSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsPreserveFontPropsToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsPreserveFontPropsToggled = value;
        }
    }
}
