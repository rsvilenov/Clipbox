using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes
{
    /// <summary>
    /// Provides configuration capability for the extensions 
    /// </summary>
    public abstract class ConfigurableExtension : 
        IConfigurationRecipient, IClipboxExtension, ISettingChangeRequestor
    {
        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by calling of the method <see cref="RequestSettingChange"/> 
        /// has been fulfilled.
        /// </summary>
        /// <param name="settingName">The name of the setting.</param>
        protected virtual void OnSettingUpdated(string settingName) { }

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
        /// Raises a request to change a setting. When the setting is changed,
        /// <see cref="ConfigurableExtension.OnSettingUpdated(string settingKeyName)"/> gets called, so that
        /// the requestor knows that the change has been applied.
        /// This method makes change requests only for settings,
        /// which are registered for the current extension type.
        /// </summary>
        protected void RequestSettingChange<TValueType>(string settingName, TValueType value)
        {
            lock (_lockObject)
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

                // prevent deadlocks, resulting in from some consumers calling GetSetting or PutSetting
                // from within this event's handler. GetSetting or PutSetting both use the same lock the current method uses.
                Task.Run(() => _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(key, value)));
            }
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
                        // Some plugins may call RequestSettingChange from the override of OnSettingUpdated,
                        // so in order to prevent deadlocks, we will run this call in a separate task, which will allow the
                        // execution of the locked code in RauseSettingChange the moment our method finishes and the lock is released.
                        Task.Run(() => OnSettingUpdated(settingKey.Name));
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
        /// <see cref="ISettingChangeRequestor.RequestSettingChange"/>
        /// </summary>
        event EventHandler<RequestSettingChangeEventArgs> ISettingChangeRequestor.RequestSettingChange
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
