using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for IncludeTextAnnotationsSettingUIPart.xaml
    /// </summary>
    public partial class IncludeTextAnnotationsSettingUIPart : SettingUIPart<bool>
    {
        public IncludeTextAnnotationsSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsIncludeTextAnnotationsToggled
        {
            get { return (bool)GetValue(IsIncludeTextAnnotationsToggledProperty); }
            set { SetValue(IsIncludeTextAnnotationsToggledProperty, value); }
        }

        public static readonly DependencyProperty IsIncludeTextAnnotationsToggledProperty =
            DependencyProperty.Register(nameof(IsIncludeTextAnnotationsToggled), typeof(bool), typeof(IncludeTextAnnotationsSettingUIPart), new PropertyMetadata(false, IsIncludeTextAnnotationsToggled_PropertyChanged));

        private static void IsIncludeTextAnnotationsToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (IncludeTextAnnotationsSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override bool GetSettingValue()
        {
            return IsIncludeTextAnnotationsToggled;
        }

        public override void SetSettingValue(bool value)
        {
            IsIncludeTextAnnotationsToggled = value;
        }
    }
}
