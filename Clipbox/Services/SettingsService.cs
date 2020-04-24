using Clipbox.Core.Configuration;
using Clipbox.Infrastructure.Configuration;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace Clipbox.Services
{
    public class SettingsService : ISettingsService
    {
        public event EventHandler<SettingsEventArgs> SettingChanged;

        private readonly IConfigurationProvider _configurationProvider;

        public SettingsService(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        public T GetSetting<T>(SettingKey<T> key)
        {
            return (T)GetSetting((ISettingKey)key);
        }

        public object GetSetting(ISettingKey key)
        {
            if (!IsSettingRegistered(key))
            {
                throw new KeyNotFoundException($"{key.Name} should be registered first.");
            }

            return _configurationProvider.GetSetting(key);
        }

        public void SetSetting<TSettingKey, TValue>(SettingKey<TSettingKey> key, TValue value) where TValue : TSettingKey
        {
            SetSetting((ISettingKey)key, value);
        }

        public void SetSetting(ISettingKey key, object value)
        {
            if (value.GetType() != key.TypeOfValue)
            {
                throw new ArgumentException($"{nameof(value)} should be of type '{key.TypeOfValue.Name}'");
            }

            if (!IsSettingRegistered(key))
            {
                throw new KeyNotFoundException($"{key.Name} should be registered first.");
            }

            _configurationProvider.SetSetting(key, value);

            SettingChanged?.Invoke(this, new SettingsEventArgs(key, value));
        }

        public void RegisterSettings(IEnumerable<ISettingKey> keys)
        {
            foreach (ISettingKey key in keys)
            {
                RegisterSetting(key);
            }
        }

        public void RegisterSetting(ISettingKey key)
        {
            if (!IsSettingRegistered(key))
            {
                _configurationProvider.RegisterSetting(key);
            }
        }

        public bool IsSettingRegistered(ISettingKey key)
        {
            return _configurationProvider.IsSettingRegistered(key);
        }
        
    }
}
