using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.ComponentModel;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for PasswordLengthSettingUIPart.xaml
    /// </summary>
    public partial class PasswordLengthSettingUIPart : SettingUIPart<string>
    {
        private int _defaultMinPasswordLen;
        private int _defaultMaxPasswordLen;
        
        public PasswordLengthSettingUIPart(int defaultMinPasswordLen, int defaultMaxPasswordLen)
        {
            _defaultMinPasswordLen = defaultMinPasswordLen;
            _defaultMaxPasswordLen = defaultMaxPasswordLen;

            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public PasswordLengthSettingUIPart()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                throw new InvalidOperationException("This constructorr is for the designer only!");
            }

            InitializeComponent();
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
            string[] valueParts = value?.Split(':');

            if (valueParts != null && valueParts.Length == 2)
            {
                MinPasswordLength = int.Parse(valueParts[0]);
                MaxPasswordLength = int.Parse(valueParts[1]);
            }
            else
            {
                MinPasswordLength = _defaultMinPasswordLen;
                MaxPasswordLength = _defaultMaxPasswordLen;
            }
        }
    }
}
