using Clipbox.Extensions.BuiltIn.TextFormats.Enums;
using Clipbox.Infrastructure.UI.Extensibility;
using System;
using System.Windows;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.SettingUIParts
{
    /// <summary>
    /// Interaction logic for PreserveFontPropsSettingUIPart.xaml
    /// </summary>
    public partial class PasswordPatternsSettingsUIPart : SettingUIPart<int>
    {
        public PasswordPatternsSettingsUIPart()
        {
            InitializeComponent();
            this.mainGrid.DataContext = this;
        }

        public bool IsPasswordPattern1Toggled
        {
            get { return (bool)GetValue(IsPasswordPattern1ToggledProperty); }
            set { SetValue(IsPasswordPattern1ToggledProperty, value); }
        }

        public static readonly DependencyProperty IsPasswordPattern1ToggledProperty =
            DependencyProperty.Register(nameof(IsPasswordPattern1Toggled), typeof(bool), typeof(PasswordPatternsSettingsUIPart), new PropertyMetadata(false, IsPasswordPattern1Toggled_PropertyChanged));

        private static void IsPasswordPattern1Toggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordPatternsSettingsUIPart)d;

            if ((bool)e.NewValue == true)
            {
                self._selectedPatternOption = PasswordMatchOptions.First;
            }

            self.SetToggleProperties();
            self.NotifySettingModified();
        }

        public bool IsPasswordPattern2Toggled
        {
            get { return (bool)GetValue(IsPasswordPattern2ToggledProperty); }
            set { SetValue(IsPasswordPattern2ToggledProperty, value); }
        }

        public static readonly DependencyProperty IsPasswordPattern2ToggledProperty =
            DependencyProperty.Register(nameof(IsPasswordPattern2Toggled), typeof(bool), typeof(PasswordPatternsSettingsUIPart), new PropertyMetadata(false, IsPasswordPattern2Toggled_PropertyChanged));

        private static void IsPasswordPattern2Toggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordPatternsSettingsUIPart)d;

            if ((bool)e.NewValue == true)
            {
                self._selectedPatternOption = PasswordMatchOptions.Second;
            }

            self.SetToggleProperties();
            self.NotifySettingModified();
        }

        public bool IsPasswordPattern3Toggled
        {
            get { return (bool)GetValue(IsPasswordPattern3ToggledProperty); }
            set { SetValue(IsPasswordPattern3ToggledProperty, value); }
        }

        public static readonly DependencyProperty IsPasswordPattern3ToggledProperty =
            DependencyProperty.Register(nameof(IsPasswordPattern3Toggled), typeof(bool), typeof(PasswordPatternsSettingsUIPart), new PropertyMetadata(false, IsPasswordPattern3Toggled_PropertyChanged));

        private static void IsPasswordPattern3Toggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordPatternsSettingsUIPart)d;

            if ((bool)e.NewValue == true)
            {
                self._selectedPatternOption = PasswordMatchOptions.Third;
            }

            self.SetToggleProperties();
            self.NotifySettingModified();
        }

        public bool IsPasswordPattern4Toggled
        {
            get { return (bool)GetValue(IsPasswordPattern4ToggledProperty); }
            set { SetValue(IsPasswordPattern4ToggledProperty, value); }
        }

        public static readonly DependencyProperty IsPasswordPattern4ToggledProperty =
            DependencyProperty.Register(nameof(IsPasswordPattern4Toggled), typeof(bool), typeof(PasswordPatternsSettingsUIPart), new PropertyMetadata(false, IsPasswordPattern4Toggled_PropertyChanged));

        private static void IsPasswordPattern4Toggled_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordPatternsSettingsUIPart)d;

            if ((bool)e.NewValue == true)
            {
                self._selectedPatternOption = PasswordMatchOptions.Fourth;
            }

            self.SetToggleProperties();
            self.NotifySettingModified();
        }

        private PasswordMatchOptions _selectedPatternOption;

        public override int GetSettingValue()
        {
            return (int)_selectedPatternOption;
        }

        public override void SetSettingValue(int value)
        {
            _selectedPatternOption = (PasswordMatchOptions)value;
            SetToggleProperties();
        }

        private void SetToggleProperties()
        {
            IsPasswordPattern1Toggled = _selectedPatternOption == PasswordMatchOptions.First;
            IsPasswordPattern2Toggled = _selectedPatternOption == PasswordMatchOptions.Second;
            IsPasswordPattern3Toggled = _selectedPatternOption == PasswordMatchOptions.Third;
            IsPasswordPattern4Toggled = _selectedPatternOption == PasswordMatchOptions.Fourth;
        }
    }
}
