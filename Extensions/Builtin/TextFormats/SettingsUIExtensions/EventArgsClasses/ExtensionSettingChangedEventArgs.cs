using System;

namespace Clipbox.Extensions.BuiltIn.TextFormats.SettingsUIExtensions.EventArgsClasses
{
    public class ExtensionSettingChangedEventArgs : EventArgs
    {
        public ExtensionSettingChangedEventArgs(string settingName, object value)
        {
            SettingName = settingName;
            Value = value;
        }

        public string SettingName { get; set; }
        
        public object Value { get; set; }
    }
}
