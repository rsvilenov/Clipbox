using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class SettingsEventArgs : EventArgs
    {
        public string SettingKey { get; set; }

        public object SettingValue { get; set; }

        public Type SettingValueType { get; set; }
    }
}
