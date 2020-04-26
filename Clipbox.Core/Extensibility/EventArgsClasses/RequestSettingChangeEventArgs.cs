using Clipbox.Core.Configuration;
using System;

namespace Clipbox.Core.Extensibility.EventArgsClasses
{
    public class RequestSettingChangeEventArgs : EventArgs
    {
        public RequestSettingChangeEventArgs(ISettingKey settingKey, object value)
        {
            SettingKey = settingKey;
            Value = value;
        }

        public ISettingKey SettingKey { get; }

        public object Value { get; }
    }
}
