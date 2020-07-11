using System;

namespace Clipbox.Views.Settings.EventArgsClasses
{
    public class SettingChangeRequestedEventArgs : EventArgs
    {
        public SettingChangeRequestedEventArgs(string settingName, object newValue)
        {
            SettingName = settingName;
            NewValue = newValue;
        }

        public string SettingName { get; }

        public object NewValue { get; }
    }
}
