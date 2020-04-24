using Clipbox.Core.Configuration;
using Clipbox.Services.EventArgsClasses;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.Interfaces
{
    public interface ISettingsService
    {
        T GetSetting<T>(SettingKey<T> key);

        object GetSetting(ISettingKey key);
        
        void SetSetting<TSettingKey, TValue>(SettingKey<TSettingKey> key, TValue value) where TValue : TSettingKey;

        bool IsSettingRegistered(ISettingKey key);

        void RegisterSetting(ISettingKey key);

        void RegisterSettings(IEnumerable<ISettingKey> keys);

        /// <summary>
        /// Get setting keys with a specified target component
        /// </summary>
        /// <typeparam name="TTargetType"></typeparam>
        /// <returns></returns>
        IEnumerable<ISettingKey> GetSettingKeys<TTargetType>();

        event EventHandler<SettingsEventArgs> SettingChanged;
    }
}
