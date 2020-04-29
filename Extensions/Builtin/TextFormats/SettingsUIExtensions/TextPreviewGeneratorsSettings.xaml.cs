using Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.EventArgsClasses;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    /// <summary>
    /// Interaction logic for TextPreviewGeneratorsSettings.xaml
    /// </summary>
    public partial class TextPreviewGeneratorsSettings : UserControl
    {
        public event EventHandler<ExtensionSettingChangedEventArgs> SettingChanged;


        public bool IsShowFormattingCharsToggled
        {
            get { return (bool)GetValue(IsShowFormattingCharsToggledProperty); }
            set { SetValue(IsShowFormattingCharsToggledProperty, value); }
        }

        public static readonly DependencyProperty IsShowFormattingCharsToggledProperty =
            DependencyProperty.Register(nameof(IsShowFormattingCharsToggled), typeof(bool), typeof(TextPreviewGeneratorsSettings), new PropertyMetadata(false, IsShowFormattingCharsToggled_PropertyChanged));

        private static void IsShowFormattingCharsToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((TextPreviewGeneratorsSettings)d);
            self.SettingChanged?.Invoke(self, new ExtensionSettingChangedEventArgs(ExtensionSettingNames.ShowFormattingCharacters, e.NewValue));
        }

        
        public bool IsCompactifyToggled
        {
            get { return (bool)GetValue(IsCompactifyToggledProperty); }
            set { SetValue(IsCompactifyToggledProperty, value); }
        }

        public static readonly DependencyProperty IsCompactifyToggledProperty =
            DependencyProperty.Register(nameof(IsCompactifyToggled), typeof(bool), typeof(TextPreviewGeneratorsSettings), new PropertyMetadata(false, IsCompactifyToggled_PropertyChanged));

        private static void IsCompactifyToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((TextPreviewGeneratorsSettings)d);
            self.SettingChanged?.Invoke(self, new ExtensionSettingChangedEventArgs(ExtensionSettingNames.Compactify, e.NewValue));
        }

        public TextPreviewGeneratorsSettings()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        
    }
}
