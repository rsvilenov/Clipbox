using System;

namespace Clipbox.Core.Extensibility.EventArgsClasses
{
    public class RequestSettingChangeEventArgs : EventArgs
    {
        public RequestSettingChangeEventArgs(string settingKey, object value)
        {
            SettingKey = SettingKey;
            Value = value;
        }

        public string SettingKey { get; }

        public object Value { get; }
    }
}
