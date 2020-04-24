using Clipbox.Core.Configuration;
using System;

namespace Clipbox.Services.EventArgsClasses
{
    public class SettingsEventArgs : EventArgs
    {
        public SettingsEventArgs(ISettingKey key, object value)
        {
            Key = key;
            Value = value;
        }

        public ISettingKey Key { get; }

        public object Value { get; }
    }
}
