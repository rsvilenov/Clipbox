using Clipbox.Core.Configuration;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes
{
    /// <summary>
    /// Provides configuration capability for the extensions 
    /// </summary>
    public abstract class ConfigurableExtension : IConfigurableObject, IClipboxExtension
    {
        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by RequestSettingChange has been fulfilled.
        /// </summary>
        /// <param name="setting"></param>
        protected virtual void OnSettingUpdated(string settingKeyName) { }
        
        /// <summary>
        /// Gets a setting value by key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="settingKeyName">The key of the setting</param>
        /// <returns>The value of the setting</returns>
        protected T GetSetting<T>(string settingName)
        {
            lock (_lockObject)
            {
                if (!_settings.ContainsKey(settingName))
                {
                    throw new KeyNotFoundException($"No setting with name {nameof(settingName)} is available.");
                }

                return (T)_settings[settingName].Value;
            }
        }
        
        // the following methods and event are to be used by the callers of the
        // extensions and should not be accessible from within the extensions.

        #region IConfigurableObject 
        void IConfigurableObject.AddSetting(string settingKeyName, SettingObject setting)
        {
            lock (_lockObject)
            {
                _settings.Add(settingKeyName, setting);
            }
        }

        void IConfigurableObject.UpdateSetting(string settingKeyName, SettingObject setting)
        {
            lock (_lockObject)
            {
                _settings[setting.KeyId] = setting;
            }

            OnSettingUpdated(settingKeyName);
        }
        #endregion
        
        private Dictionary<string, SettingObject> _settings = new Dictionary<string, SettingObject>();

        private readonly object _lockObject = new object();
    }
}
