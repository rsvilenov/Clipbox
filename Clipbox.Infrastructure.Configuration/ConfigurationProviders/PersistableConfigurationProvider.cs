using Clipbox.Core.Configuration;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Clipbox.Infrastructure.Configuration
{
    public class PersistableConfigurationProvider : IConfigurationProvider
    {
        private const string SETTINGS_PERSISTENCE_CATEGORY = "Settings";

        private readonly IPersistenceProvider _persistenceProvider;

        private readonly Dictionary<string, SettingObject> _settings;

        private readonly object _lockHandle = new object();
        
        public PersistableConfigurationProvider(IPersistenceProvider persistenceProvider)
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
                string keyId = GetKeyStableHash(settingKey);
                if (_settings.ContainsKey(keyId))
                {
                    throw new InvalidOperationException($"Setting '{settingKey.Name}' is already registered.");
                }

                _settings.Add(keyId,  
                    new SettingObject(keyId, settingKey.DefaultValue, SettingObject.GetSettingValueType(settingKey.TypeOfValue)));
            }
        }

        public bool IsSettingRegistered(ISettingKey settingKey)
        {
            return _settings.ContainsKey(GetKeyStableHash(settingKey));
        }

        public void SetSetting(ISettingKey settingKey, object value)
        {
            lock (_lockHandle)
            {
                string keyId = GetKeyStableHash(settingKey);
                if (!_settings.ContainsKey(keyId))
                {
                    throw new KeyNotFoundException($"There is no setting for the key passed.");
                }

                _settings[keyId] = new SettingObject(keyId, value, _settings[keyId].ValueType);
                StoreSetting(_settings[keyId]);
            }
        }

        public object GetSetting(ISettingKey settingKey)
        {
            lock (_lockHandle)
            {
                string keyId = GetKeyStableHash(settingKey);
                if (!_settings.ContainsKey(keyId))
                {
                    throw new KeyNotFoundException($"There is no setting for the key passed.");
                }

                return _settings[keyId].Value;
            }
        }

        private Dictionary<string, SettingObject> FetchAllSettings()
        {
            var settings = new Dictionary<string, SettingObject>();

            foreach (string storedSetting in _persistenceProvider.ListKeys(SETTINGS_PERSISTENCE_CATEGORY))
            {
                SettingObject settingObject = DeserializeSettingObject(_persistenceProvider.ReadBytes(storedSetting, SETTINGS_PERSISTENCE_CATEGORY));
                settings.Add(settingObject.KeyId, settingObject);
            }

            return settings;
        }

        private void StoreSetting(SettingObject settingObject)
        {
            _persistenceProvider.Write(settingObject.KeyId, SerializeObject(settingObject), SETTINGS_PERSISTENCE_CATEGORY);
        }

        /// <summary>
        /// Generate a unique and stable key string from the ISettingKey
        /// </summary>
        private string GetKeyStableHash(ISettingKey key)
        {
            return MD5Hash.Create(string.Concat(key.Name, key.TargetComponentType?.FullName ?? string.Empty));
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
