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
        
        void SetSetting(ISettingKey key, object value);
        
        bool IsSettingRegistered(ISettingKey key);

        void RegisterSetting(ISettingKey key);

        void RegisterSettings(IEnumerable<ISettingKey> keys);

        /// <summary>
        /// Get setting keys with a specified target component
        /// </summary>
        /// <typeparam name="TTargetType"></typeparam>
        /// <returns></returns>
        IEnumerable<ISettingKey> GetSettingKeys<TTargetType>();

        IEnumerable<ISettingKey> GetSettingKeys(Type targetType);
        
        event EventHandler<SettingsEventArgs> SettingChanged;
    }
}
