using Clipbox.Core.Configuration;
using Clipbox.Services.EventArgsClasses;
using Clipbox.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

namespace Clipbox.Services
{
    public class SettingsService : ISettingsService
    {
        public event EventHandler<SettingsEventArgs> SettingChanged;

        public SettingsService()
        {
            ApplicationSettings.Default.Upgrade();
        }

        public T GetSetting<T>(SettingKey<T> key)
        {
            if (ApplicationSettings.Default[key.Name] == null)
            {
                throw new KeyNotFoundException($"{key.Name} should be defined in ApplicationSettings in order to be used by this class.");
            }

            var setting = ApplicationSettings.Default[key.Name];

            if (setting.GetType() != typeof(T))
            {
                throw new InvalidCastException($"Setting {key.Name} is not of type {typeof(T)}");
            }

            return (T)setting;
        }

        public void SetSetting<TSettingKey, TValue>(SettingKey<TSettingKey> key, TValue value) where TValue : TSettingKey
        {
            if (ApplicationSettings.Default[key.Name] == null)
            {
                throw new KeyNotFoundException($"{key.Name} should be defined in ApplicationSettings in order to be used by this class.");
            }

            bool isUserSetting = ApplicationSettings.Default.Properties[key.Name].Attributes.ContainsKey(typeof(System.Configuration.UserScopedSettingAttribute));
            if (!isUserSetting)
            {
                throw new InvalidOperationException($"{key.Name} 's scope must be 'user'. Settings with scope 'application' can not be saved.");
            }

            ApplicationSettings.Default[key.Name] = value;

            SettingChanged?.Invoke(this,
                new SettingsEventArgs
                {
                    SettingKeyName = key.Name,
                    SettingValue = value,
                    SettingValueType = typeof(TValue)
                });

            ApplicationSettings.Default.Save();
        }

        public IReadOnlyDictionary<string, Type> GetAvailableSettings()
        {
            Dictionary<string, Type> availableSettings = new Dictionary<string, Type>();

            foreach (SettingsProperty prop in ApplicationSettings.Default.Properties)
            {
                availableSettings.Add(prop.Name, prop.PropertyType);
            }

            return new ReadOnlyDictionary<string, Type>(availableSettings);
        }
    }
}
