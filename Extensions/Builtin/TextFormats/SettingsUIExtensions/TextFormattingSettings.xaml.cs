using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    /// <summary>
    /// Interaction logic for TextFormattingSettings.xaml
    /// </summary>
    public partial class TextFormattingSettings : UserControl
    {
        public event EventHandler SettingChanged;

        
        public bool IsSaveSeparatorToggled
        {
            get { return (bool)GetValue(IsSaveSeparatorToggledProperty); }
            set { SetValue(IsSaveSeparatorToggledProperty, value); }
        }

        public static readonly DependencyProperty IsSaveSeparatorToggledProperty =
            DependencyProperty.Register(nameof(IsSaveSeparatorToggled), typeof(bool), typeof(TextFormattingSettings), new PropertyMetadata(false, IsSaveSeparatorToggled_PropertyChanged));

        private static void IsSaveSeparatorToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((TextFormattingSettings)d);
            self.SettingChanged?.Invoke(self, new EventArgs());
        }

        public TextFormattingSettings()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }
    }
}
