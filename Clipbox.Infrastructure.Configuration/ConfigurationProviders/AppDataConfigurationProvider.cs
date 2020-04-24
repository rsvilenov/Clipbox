using Clipbox.Core.Configuration;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.Infrastructure.Configuration
{
    public class AppDataConfigurationProvider : IConfigurationProvider
    {
        private const string SETTINGS_PERSISTENCE_CATEGORY = "Settings";

        private readonly IPersistenceProvider _persistenceProvider;

        private readonly Dictionary<string, SettingObject> _settings;

        private readonly object _lockHandle = new object();
        
        public AppDataConfigurationProvider(IPersistenceProvider persistenceProvider)
        {
            _persistenceProvider = persistenceProvider;

            if (!_persistenceProvider.CheckIfCategoryExists(SETTINGS_PERSISTENCE_CATEGORY))
            {
                _persistenceProvider.CreateCategory(SETTINGS_PERSISTENCE_CATEGORY);
            }

            _settings = FetchAllSettings();
        }
        
        public void RegisterSetting(ISettingKey settingKey)
        {
            lock (_lockHandle)
            {
                if (_settings.ContainsKey(settingKey.Name))
                {
                    throw new InvalidOperationException($"Setting '{settingKey.Name}' is already registered.");
                }

                _settings.Add(settingKey.Name,  
                    new SettingObject(settingKey.Name, settingKey.DefaultValue, SettingObject.GetSettingValueType(settingKey.TypeOfValue)));
            }
        }

        public bool IsSettingRegistered(ISettingKey settingKey)
        {
            return _settings.ContainsKey(settingKey.Name);
        }

        public void SetSetting(ISettingKey settingKey, object value)
        {
            lock (_lockHandle)
            {
                if (_settings.ContainsKey(settingKey.Name))
                {
                    throw new KeyNotFoundException($"There is no setting with key name {settingKey.Name}");
                }

                _settings[settingKey.Name] = new SettingObject(settingKey.Name, value, _settings[settingKey.Name].ValueType);
                StoreSetting(_settings[settingKey.Name]);
            }
        }

        public object GetSetting(ISettingKey settingKey)
        {
            lock (_lockHandle)
            {
                if (_settings.ContainsKey(settingKey.Name))
                {
                    throw new KeyNotFoundException($"There is no setting with key name {settingKey.Name}");
                }

                return _settings[settingKey.Name].Value;
            }
        }

        private Dictionary<string, SettingObject> FetchAllSettings()
        {
            var settings = new Dictionary<string, SettingObject>();

            foreach (string storedSetting in _persistenceProvider.ListKeys(SETTINGS_PERSISTENCE_CATEGORY))
            {
                SettingObject settingObject = DeserializeSettingObject(_persistenceProvider.ReadBytes(storedSetting, SETTINGS_PERSISTENCE_CATEGORY));
                settings.Add(settingObject.KeyName, settingObject);
            }

            return settings;
        }

        private void StoreSetting(SettingObject settingObject)
        {
            _persistenceProvider.Write(settingObject.KeyName, SerializeObject(settingObject), SETTINGS_PERSISTENCE_CATEGORY);
        }

        private byte[] SerializeObject(SettingObject settingObject)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, settingObject);
                ms.Flush();
                return ms.ToArray();
            }
        }

        private SettingObject DeserializeSettingObject(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (SettingObject)formatter.Deserialize(ms);
            }
        }
    }
}
