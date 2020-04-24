using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes
{
    /// <summary>
    /// Provides configuration capability for the extensions 
    /// </summary>
    public abstract class ConfigurableExtension : IConfigurableObject, IClipboxExtention
    {
        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by RequestSettingChange has been fulfilled.
        /// </summary>
        /// <param name="setting"></param>
        protected virtual void OnSettingUpdated(string settingKeyName) { }

        private List<SettingObject> _settings = new List<SettingObject>();

        
        /// <summary>
        /// Gets a setting value by key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="settingKeyName">The key of the setting</param>
        /// <returns>The value of the setting</returns>
        protected T GetSetting<T>(string settingName)
        {
            return (T)_settings.First(s => s.KeyName == settingName).Value;
        }
        
        // the following methods and event are to be used by the callers of the
        // extensions and should not be accessible from within the extensions.

        #region IConfigurableObject 
        void IConfigurableObject.LoadSettings(IEnumerable<SettingObject> settings)
        {
            _settings.AddRange(settings);
        }

        void IConfigurableObject.UpdateSetting(SettingObject setting)
        {
            var oldObject = _settings.First(s => s.KeyName == setting.KeyName);
            _settings.Remove(oldObject);
            _settings.Add(setting);
        }
        #endregion
    }
}
