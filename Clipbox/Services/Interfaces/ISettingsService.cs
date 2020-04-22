using Clipbox.Core.Configuration;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface ISettingsService
    {
        T GetSetting<T>(SettingKey<T> key);

        IReadOnlyDictionary<string, Type> GetAvailableSettings();

        void SetSetting<TSettingKey, TValue>(SettingKey<TSettingKey> key, TValue value) where TValue : TSettingKey;

        event EventHandler<SettingsEventArgs> SettingChanged;
    }
}
