using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes
{
    /// <summary>
    /// Provides configuration capability for the extensions 
    /// </summary>
    public abstract class ConfigurableExtension : IConfigurableObject, IConfigurationChangeRequestor, IClipboxExtention
    {
        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by RequestSettingChange has been fulfilled.
        /// </summary>
        /// <param name="setting"></param>
        protected virtual void OnSettingUpdated(string settingKey) { }
        
        private List<SettingObject> _settings = new List<SettingObject>();

        /// <summary>
        /// Gets a setting value by key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value</typeparam>
        /// <param name="settingKey">The key of the setting</param>
        /// <returns>The value of the setting</returns>
        protected T GetSetting<T>(string settingKey)
        {
            EnsureParameterTypeMatchesSettingType<T>(settingKey);

            return (T)_settings.First(s => s.Key == settingKey).Value;
        }

        /// <summary>
        /// Raises a request to change a setting. When the setting is changed,
        /// <see cref="ConfigurableExtension.OnSettingUpdated(SettingObject)"/> gets called, so that
        /// the requestor knows that the change has been applied.
        /// </summary>
        protected void RaiseRequestSettingChange<T>(string settingKey, T value)
        {
            EnsureParameterTypeMatchesSettingType<T>(settingKey);

            _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(settingKey, value));
        }

        private void EnsureParameterTypeMatchesSettingType<T>(string settingKey)
        {
            if (_settings.Any(s => s.Key == settingKey
                        && s.GetClrType(s.ValueType) != typeof(T)))
            {
                throw new ArgumentException($"The setting with key {nameof(settingKey)} has a different type than that passed as a generic parameter.");
            }
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
            var oldObject = _settings.First(s => s.Key == setting.Key);
            _settings.Remove(oldObject);
            _settings.Add(setting);
        }
        #endregion

        #region IConfigurationChangeRequestor
        private event EventHandler<RequestSettingChangeEventArgs> _requestSettingChange;
        /// <summary>
        /// Raises a request to change a setting. 
        /// </summary>
        event EventHandler<RequestSettingChangeEventArgs> IConfigurationChangeRequestor.RequestSettingChange
        {
            add
            {
                _requestSettingChange += value;
            }

            remove
            {
                _requestSettingChange -= value;
            }
        }
        #endregion

    }
}
