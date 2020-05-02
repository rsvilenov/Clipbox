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



        public bool IsPreserveOriginalFontPropertiesToggled
        {
            get { return (bool)GetValue(IsPreserveOriginalFontPropertiesToggledProperty); }
            set { SetValue(IsPreserveOriginalFontPropertiesToggledProperty, value); }
        }

        public static readonly DependencyProperty IsPreserveOriginalFontPropertiesToggledProperty =
            DependencyProperty.Register(nameof(IsPreserveOriginalFontPropertiesToggled), typeof(bool), typeof(TextPreviewGeneratorsSettings), new PropertyMetadata(false, IsPreserveOriginalFontPropertiesToggled_PropertyChanged));

        private static void IsPreserveOriginalFontPropertiesToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((TextPreviewGeneratorsSettings)d);
            self.SettingChanged?.Invoke(self, new ExtensionSettingChangedEventArgs(ExtensionSettingNames.PreserveOriginalFontProperties, e.NewValue));
        }



        public double SelectedFontSize
        {
            get { return (double)GetValue(SelectedFontSizeProperty); }
            set { SetValue(SelectedFontSizeProperty, value); }
        }

        public static readonly DependencyProperty SelectedFontSizeProperty =
            DependencyProperty.Register(nameof(SelectedFontSize), typeof(double), typeof(TextPreviewGeneratorsSettings), new PropertyMetadata(0d, SelectedFontSize_PropertyChanged));

        private static void SelectedFontSize_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((TextPreviewGeneratorsSettings)d);
            self.SettingChanged?.Invoke(self, new ExtensionSettingChangedEventArgs(ExtensionSettingNames.FontScale, e.NewValue));
        }

        public TextPreviewGeneratorsSettings()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        
    }
}
