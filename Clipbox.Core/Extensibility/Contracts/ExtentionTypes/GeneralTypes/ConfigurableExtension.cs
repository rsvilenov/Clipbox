using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes
{
    /// <summary>
    /// Provides configuration capability for the extensions 
    /// </summary>
    public abstract class ConfigurableExtension : 
        IConfigurationRecipient, IClipboxExtension, IConfigurationChangeRequestor
    {
        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by calling of the method <see cref="RequestSettingChange"/> 
        /// has been fulfilled.
        /// </summary>
        /// <param name="settingName">The name of the setting.</param>
        /// <param name="targetType">The extension type, for which the value is registered.</param>
        protected virtual void OnSettingUpdated(string settingName, Type targetType) { }

        /// <summary>
        /// Gets a setting value by key.
        /// This method makes change requests only for settings,
        /// which are registered for the current extension type.
        /// </summary>
        /// <typeparam name="TValueType">The type of the setting value</typeparam>
        /// <param name="settingName">The name of the setting</param>
        /// <returns>The value of the setting</returns>
        protected TValueType GetSetting<TValueType>(string settingName)
        {
            lock (_lockObject)
            {
                if (!(_settings.Any(s => s.Key.Name == settingName 
                    && s.Key.TargetComponentType == GetType())))
                {
                    throw new ArgumentException($"Setting with name '{settingName}' is not registered for this extention type.");
                }

                return (TValueType)_settings
                    .Single(s => s.Key.Name == settingName
                        && s.Key.TargetComponentType == GetType())
                    .Value;
            }
        }

        /// <summary>
        /// Gets the value of a setting.
        /// </summary>
        /// <typeparam name="TValueType">The type of the setting value.</typeparam>
        /// <typeparam name="TExtensionType">The extension type, for which the value is registered.</typeparam>
        /// <param name="settingName">The name of the setting</param>
        /// <returns>The value of the setting.</returns>
        protected TValueType GetSetting<TValueType, TExtensionType>(string settingName)
        {
            lock (_lockObject)
            {
                if (!(_settings.Any(s => s.Key.Name == settingName
                    && s.Key.TargetComponentType == typeof(TExtensionType))))
                {
                    throw new ArgumentException($"Setting with name '{settingName}' is not registered for extention type {typeof(TExtensionType).Name}.");
                }

                return (TValueType)_settings
                    .Single(s => s.Key.Name == settingName
                        && s.Key.TargetComponentType == typeof(TExtensionType))
                    .Value;
            }
        }

        /// <summary>
        /// Raises a request to change a setting. When the setting is changed,
        /// <see cref="ConfigurableExtension.OnSettingUpdated(string settingKeyName, Type targetType)"/> gets called, so that
        /// the requestor knows that the change has been applied.
        /// </summary>
        protected void RequestSettingChange<TValueType, TExtensionType>(string settingName, TValueType value)
        {
            if (!(_settings.Any(s => s.Key.Name == settingName 
                && s.Key.TargetComponentType == typeof(TExtensionType))))
            {
                throw new ArgumentException($"Setting with name '{settingName}' is not registered for extention type {typeof(TExtensionType).Name}.");
            }

            ISettingKey key = _settings.Single(s => s.Key.Name == settingName
                && s.Key.TargetComponentType == typeof(TExtensionType))
                .Key;

            _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(key, value));
        }

        /// <summary>
        /// Raises a request to change a setting. When the setting is changed,
        /// <see cref="ConfigurableExtension.OnSettingUpdated(string settingKeyName, Type targetType)"/> gets called, so that
        /// the requestor knows that the change has been applied.
        /// This method makes change requests only for settings,
        /// which are registered for the current extension type.
        /// </summary>
        protected void RequestSettingChange<TValueType>(string settingName, TValueType value)
        {
            if (!_settings.Any(s => s.Key.Name == settingName 
                && s.Key.TargetComponentType == GetType()))
            {
                throw new ArgumentException($"Setting with name '{settingName}' is not registered for this extention type.");
            }
            

            ISettingKey key = _settings
                .Single(s => s.Key.Name == settingName
                    && s.Key.TargetComponentType == GetType())
                .Key;

            _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(key, value));
        }

        // the following methods and event are to be used by the callers of the
        // extensions and should not be accessible from within the extensions.

        #region IConfigurationRecipient
        /// <summary>
        /// <see cref="IConfigurationRecipient.PutSetting(ISettingKey, object)"/>
        /// </summary>
        void IConfigurationRecipient.PutSetting(ISettingKey settingKey, object settingValue)
        {
            lock (_lockObject)
            {
                if (_settings.ContainsKey(settingKey))
                {
                    object oldValue = _settings[settingKey];

                    _settings[settingKey] = settingValue;

                    if (oldValue != settingValue)
                    {
                        OnSettingUpdated(settingKey.Name, settingKey.TargetComponentType);
                    }
                }
                else
                {
                    _settings.Add(settingKey, settingValue);
                }
            }
        }

        private event EventHandler<RequestSettingChangeEventArgs> _requestSettingChange;
        /// <summary>
        /// <see cref="IConfigurationRecipient.RequestSettingChange"/>
        /// </summary>
        event EventHandler<RequestSettingChangeEventArgs> IConfigurationRecipient.RequestSettingChange
        {
            add
            {
                _requestSettingChange -= value;
                _requestSettingChange += value;
            }

            remove
            {
                _requestSettingChange -= value;
            }
        }
        #endregion

        private Dictionary<ISettingKey, object> _settings = new Dictionary<ISettingKey, object>();

        private readonly object _lockObject = new object();
    }
}
