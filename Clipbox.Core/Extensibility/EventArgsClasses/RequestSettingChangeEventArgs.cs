using Clipbox.Core.Configuration;
using System;

namespace Clipbox.Core.Extensibility.EventArgsClasses
{
    public class RequestSettingChangeEventArgs : EventArgs
    {
        public RequestSettingChangeEventArgs(string settingName, object value)
        {
            SettingName = settingName;
            Value = value;
        }

        public string SettingName { get; }

        public object Value { get; }
    }
}
