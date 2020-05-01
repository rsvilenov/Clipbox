using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Infrastructure.UI.Extensibility;
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

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for DefaultSeparatorSettingUIPart.xaml
    /// </summary>
    public partial class DefaultSeparatorSettingUIPart : SettingUIPart<int>
    {
        private TextConcatenateSeparators _currentSeparator;

        public DefaultSeparatorSettingUIPart()
        {
            InitializeComponent();
            this.mainStackPanel.DataContext = this;
            mtsaUiExt.SeparatorChanged += MtsaUiExt_SeparatorChanged;
        }

        private void MtsaUiExt_SeparatorChanged(object sender, UIExtensions.MultipleTextSelectionsAlignment.SeparatorEventArgs e)
        {
            _currentSeparator = e.Separator;
            NotifySettingModified();
        }

        public override int GetSettingValue()
        {
            return (int)_currentSeparator;
        }

        public override void SetSettingValue(int value)
        {
            TextConcatenateSeparators seprator = (TextConcatenateSeparators)value;
            mtsaUiExt.SetSeparatorType(seprator);
            _currentSeparator = seprator;
        }

        public void SetIsVisible(bool isVisible)
        {
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
