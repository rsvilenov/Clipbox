using Clipbox.Core.Extensibility.Enums;
using Clipbox.Views.Settings.EventArgsClasses;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Clipbox.Views.Settings
{
    public abstract class SettingsWindowUIPart<TSettingValue> : UserControl
    {
        public event EventHandler<SettingChangeRequestedEventArgs> SettingChangeRequested;

        private readonly string _settingName;

        /// <summary>
        /// This constructor is to be called by the constructor in the inheriting class,
        /// which has no arguments and is there for the designer to work.
        /// </summary>
        /// <param name="_"></param>
        protected SettingsWindowUIPart(object _) { }

        public SettingsWindowUIPart(string settingName, object settingValue)
        {
            _settingName = settingName;
            SettingValue = (TSettingValue)settingValue;
        }

        public abstract SettingsCategory Category { get; }

        protected TSettingValue SettingValue
        {
            get { return (TSettingValue)GetValue(SettingValueProperty); }
            set { SetValue(SettingValueProperty, value); }
        }

        protected static readonly DependencyProperty SettingValueProperty =
            DependencyProperty.Register(nameof(SettingValue), typeof(TSettingValue), typeof(SettingsWindowUIPart<TSettingValue>), new PropertyMetadata(default(TSettingValue), SettingValue_PropertyChanged));

        private static void SettingValue_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SettingsWindowUIPart<TSettingValue> self = (SettingsWindowUIPart<TSettingValue>)d;
            self.SettingChangeRequested?.Invoke(self, new SettingChangeRequestedEventArgs(self._settingName, e.NewValue));
        }
    }
}
