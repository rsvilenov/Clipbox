using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface ISettingsService
    {
        T GetSetting<T>(string key);

        IReadOnlyDictionary<string, Type> GetAvailableSettings();

        void SetSetting<T>(string key, T value);

        event EventHandler<SettingsEventArgs> SettingChanged;
    }
}
