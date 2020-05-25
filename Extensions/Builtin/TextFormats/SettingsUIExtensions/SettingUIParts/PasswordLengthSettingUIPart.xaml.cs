using Clipbox.Infrastructure.UI.Extensibility;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for SkipShortStringsSettingsUIPart.xaml
    /// </summary>
    public partial class PasswordLengthSettingUIPart : SettingUIPart<string>
    {
        const int DEFAULT_MIN_PASSWORD_LEN = 8;
        const int DEFAULT_MAX_PASSWORD_LEN = 20;

        public PasswordLengthSettingUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public int? MinPasswordLength
        {
            get { return (int?)GetValue(MinPasswordLengthProperty); }
            set { SetValue(MinPasswordLengthProperty, value); }
        }

        public static readonly DependencyProperty MinPasswordLengthProperty =
            DependencyProperty.Register(nameof(MinPasswordLength), typeof(int?), typeof(PasswordLengthSettingUIPart), new PropertyMetadata(0, MinPasswordLength_PropertyChanged));

        private static void MinPasswordLength_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordLengthSettingUIPart)d;
            self.NotifySettingModified();
        }

        public int? MaxPasswordLength
        {
            get { return (int?)GetValue(MaxPasswordLengthProperty); }
            set { SetValue(MaxPasswordLengthProperty, value); }
        }

        public static readonly DependencyProperty MaxPasswordLengthProperty =
            DependencyProperty.Register(nameof(MaxPasswordLength), typeof(int?), typeof(PasswordLengthSettingUIPart), new PropertyMetadata(0, MaxPasswordLength_PropertyChanged));

        private static void MaxPasswordLength_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordLengthSettingUIPart)d;
            self.NotifySettingModified();
        }

        public override string GetSettingValue()
        {
            return $"{MinPasswordLength}:{MaxPasswordLength}";
        }

        public override void SetSettingValue(string value)
        {
            if (value != null)
            {
                string[] valueParts = value.Split(':');
                if (valueParts.Length == 2)
                {
                    MinPasswordLength = int.Parse(valueParts[0]);
                    MaxPasswordLength = int.Parse(valueParts[1]);
                }
            }

            if (MaxPasswordLength == 0)
            {
                MinPasswordLength = DEFAULT_MIN_PASSWORD_LEN;
                MaxPasswordLength = DEFAULT_MAX_PASSWORD_LEN;
            }
        }
        
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (MinPasswordLength == null)
            {
                MinPasswordLength = 0;
            }

            if (MaxPasswordLength == null)
            {
                MaxPasswordLength = 0;
            }
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;

            if (e.Text != null
                && (e.Text.All(c => char.IsNumber(c)) == false
                    || (tb.Text != null && tb.Text.Length + e.Text.Length > 2) /* 99 letters max */))
            {
                e.Handled = true;
            }
        }
    }
}
