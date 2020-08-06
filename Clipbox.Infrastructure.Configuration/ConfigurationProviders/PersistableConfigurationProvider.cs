using Clipbox.Core.Configuration;
using Clipbox.Infrastructure.Encryption;
using Clipbox.Infrastructure.Persistence;
using Clipbox.Infrastructure.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

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

            _settings = new Dictionary<string, SettingObject>();
        }
        
        public void RegisterSetting(ISettingKey settingKey)
        {
            lock (_lockHandle)
            {
                if (!_settings.Any())
                {
                    LoadAllSettings();
                }

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
            lock (_lockHandle)
            {
                if (!_settings.Any())
                {
                    LoadAllSettings();
                }

                return _settings.ContainsKey(GetKeyStableHash(settingKey));
            }
        }

        public void SetSetting(ISettingKey settingKey, object value)
        {
            lock (_lockHandle)
            {
                if (!_settings.Any())
                {
                    LoadAllSettings();
                }

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
                if (!_settings.Any())
                {
                    LoadAllSettings();
                }

                string keyId = GetKeyStableHash(settingKey);
                if (!_settings.ContainsKey(keyId))
                {
                    throw new KeyNotFoundException($"There is no setting for the key passed.");
                }

                return _settings[keyId].Value;
            }
        }

        private void LoadAllSettings()
        {
            foreach (string storedSetting in _persistenceProvider.ListKeys(SETTINGS_PERSISTENCE_CATEGORY))
            {
                SettingObject settingObject = Deserialize(_persistenceProvider.ReadBytes(storedSetting, SETTINGS_PERSISTENCE_CATEGORY));
                _settings.Add(settingObject.KeyId, settingObject);
            }
        }

        private void StoreSetting(SettingObject settingObject)
        {
            _persistenceProvider.Write(settingObject.KeyId, Serialize(settingObject), SETTINGS_PERSISTENCE_CATEGORY);
        }

        /// <summary>
        /// Generate a unique and stable key string from the ISettingKey
        /// </summary>
        protected virtual string GetKeyStableHash(ISettingKey key)
        {
            return MD5Hash.Create(string.Concat(key.Name, key.TargetComponentType?.FullName ?? string.Empty));
        }

        /// <summary>
        /// Creates a byte array out of a setting object
        /// </summary>
        /// <param name="settingObject"></param>
        /// <returns></returns>
        protected virtual byte[] Serialize(SettingObject settingObject)
        {
            return BinaryFormatterSerialization.SerializeObject(settingObject);
        }

        /// <summary>
        /// Creates a setting object out of a serialized byte array
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        protected virtual SettingObject Deserialize(byte[] serialized)
        {
            return BinaryFormatterSerialization.DeserializeObject<SettingObject>(serialized);
        }
    }
}
