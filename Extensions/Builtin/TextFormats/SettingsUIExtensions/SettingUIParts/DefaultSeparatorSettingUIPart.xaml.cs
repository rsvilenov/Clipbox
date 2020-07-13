using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Extensions.BuiltIn.TextFormats.FlowUIExtensions.UIControls;
using Clipbox.Infrastructure.UI.Extensibility;
using System.Windows;

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

        private void MtsaUiExt_SeparatorChanged(object sender, MultipleTextSelectionsAlignment.SeparatorEventArgs e)
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
    }
}
