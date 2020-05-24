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
                self._patternOptions |= PasswordMatchOptions.First;
            }
            else
            {
                self._patternOptions &= ~PasswordMatchOptions.First;
            }

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
                self._patternOptions |= PasswordMatchOptions.Second;
            }
            else
            {
                self._patternOptions &= ~PasswordMatchOptions.Second;
            }

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
                self._patternOptions |= PasswordMatchOptions.Third;
            }
            else
            {
                self._patternOptions &= ~PasswordMatchOptions.Third;
            }

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
                self._patternOptions |= PasswordMatchOptions.Fourth;
            }
            else
            {
                self._patternOptions &= ~PasswordMatchOptions.Fourth;
            }

            self.NotifySettingModified();
        }

        private PasswordMatchOptions _patternOptions;

        public override int GetSettingValue()
        {
            return (int)_patternOptions;
        }

        public override void SetSettingValue(int value)
        {
            _patternOptions = (PasswordMatchOptions)value;

            IsPasswordPattern1Toggled = _patternOptions.HasFlag(PasswordMatchOptions.First);
            IsPasswordPattern2Toggled = _patternOptions.HasFlag(PasswordMatchOptions.Second);
            IsPasswordPattern3Toggled = _patternOptions.HasFlag(PasswordMatchOptions.Third);
            IsPasswordPattern4Toggled = _patternOptions.HasFlag(PasswordMatchOptions.Fourth);
        }
    }
}
