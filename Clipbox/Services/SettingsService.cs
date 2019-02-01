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

        public T GetSetting<T>(string key)
        {
            if (ApplicationSettings.Default[key] == null)
            {
                throw new KeyNotFoundException($"{key} should be defined in ApplicationSettings in order to be used by this class.");
            }

            var setting = ApplicationSettings.Default[key];

            if (setting.GetType() != typeof(T))
            {
                throw new InvalidCastException($"Setting {key} is not of type {typeof(T)}");
            }

            return (T)setting;
        }

        public void SetSetting<T>(string key, T value)
        {
            if (ApplicationSettings.Default[key] == null)
            {
                throw new KeyNotFoundException($"{key} should be defined in ApplicationSettings in order to be used by this class.");
            }

            bool isUserSetting = ApplicationSettings.Default.Properties[key].Attributes.ContainsKey(typeof(System.Configuration.UserScopedSettingAttribute));
            if (!isUserSetting)
            {
                throw new InvalidOperationException($"{key} 's scope must be 'user'. Settings with scope 'application' can not be saved.");
            }

            ApplicationSettings.Default[key] = value;

            SettingChanged?.Invoke(this,
                new SettingsEventArgs
                {
                    SettingKey = key,
                    SettingValue = value,
                    SettingValueType = typeof(T)
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
