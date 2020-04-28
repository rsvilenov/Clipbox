using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions
{
    /// <summary>
    /// Interaction logic for TextPreviewGeneratorsSettings.xaml
    /// </summary>
    public partial class TextPreviewGeneratorsSettings : UserControl
    {
        public event EventHandler SettingChanged;


        public bool IsShowFormattingCharsToggled
        {
            get { return (bool)GetValue(IsShowFormattingCharsToggledProperty); }
            set { SetValue(IsShowFormattingCharsToggledProperty, value); }
        }

        public static readonly DependencyProperty IsShowFormattingCharsToggledProperty =
            DependencyProperty.Register(nameof(IsShowFormattingCharsToggled), typeof(bool), typeof(TextPreviewGeneratorsSettings), new PropertyMetadata(false, IsSaveSeparatorToggled_PropertyChanged));

        private static void IsSaveSeparatorToggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = ((TextPreviewGeneratorsSettings)d);
            self.SettingChanged?.Invoke(self, new EventArgs());
        }

        public TextPreviewGeneratorsSettings()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}
