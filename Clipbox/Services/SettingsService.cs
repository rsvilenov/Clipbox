using Clipbox.Core.Configuration;
using Clipbox.Infrastructure.Configuration;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Services
{
    public class SettingsService : ISettingsService
    {
        public event EventHandler<SettingsEventArgs> SettingChanged;

        private readonly IConfigurationProvider _configurationProvider;

        private readonly List<ISettingKey> _targettedSettingKeys;

        public SettingsService(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
            _targettedSettingKeys = new List<ISettingKey>();
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

            if (key.TargetComponentType != null &&
                !_targettedSettingKeys.Contains(key))
            {
                _targettedSettingKeys.Add(key);
            }
        }

        public bool IsSettingRegistered(ISettingKey key)
        {
            return _configurationProvider.IsSettingRegistered(key);
        }

        public IEnumerable<ISettingKey> GetSettingKeys<TTargetType>()
        {
            return GetSettingKeys(typeof(TTargetType));
        }

        public IEnumerable<ISettingKey> GetSettingKeys(Type targetType)
        {
            return _targettedSettingKeys.Where(tsk => tsk.TargetComponentType == targetType);
        }
    }
}
