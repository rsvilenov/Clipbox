using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes
{
    /// <summary>
    /// Settings view extension
    /// </summary>
    public abstract class SettingsUIExtension : IClipboxExtension, ISettingsRegistrator, IConfigurationRecipient, ISettingChangeRequestor
    {   
        public SettingsUIExtension()
        {
            var settingKeyColBuilder = new SettingKeyCollectionBuilder();
            RegisterSettingKeys(settingKeyColBuilder);
            _registeredSettingKeys = settingKeyColBuilder.GetSettingKeys();
        }

        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by calling of the method <see cref="RequestSettingChange"/> 
        /// has been fulfilled.
        /// </summary>
        /// <param name="settingName">The name of the setting.</param>
        /// <param name="targetType">The extension type, for which the value is registered.</param>
        protected virtual void OnSettingUpdated(string settingName, Type targetType) { }

        /// <summary>
        /// Create and return the part of the Settings view,
        /// corresponding to the settings for an extension. 
        /// </summary>
        /// <returns></returns>
        public abstract UIElement GetSettingsViewPart();

        /// <summary>
        /// Register keys for the settings 
        /// that will be used by the consuming extensions.
        /// </summary>
        protected abstract void RegisterSettingKeys(SettingKeyCollectionBuilder settingKeyColBuilder);

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
        /// <see cref="SettingsUIExtension.OnSettingUpdated(string settingKeyName, Type targetType)"/> gets called, so that
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

        #region ISettingsRegistrator

        IEnumerable<ISettingKey> ISettingsRegistrator.GetSettingKeys()
        {
            return _registeredSettingKeys;
        }

        #endregion

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
        
        private readonly IEnumerable<ISettingKey> _registeredSettingKeys;

        private Dictionary<ISettingKey, object> _settings = new Dictionary<ISettingKey, object>();

        private readonly object _lockObject = new object();


        protected class SettingKeyCollectionBuilder
        {
            private readonly List<ISettingKey> _settingKeys;

            public SettingKeyCollectionBuilder()
            {
                _settingKeys = new List<ISettingKey>();
            }

            public SettingKeyCollectionBuilder Add<TValueType, TTargetExtensionType>(string keyName, TValueType defaultValue)
                where TTargetExtensionType : ConfigurableExtension
            {
                _settingKeys.Add(new SettingKey<TValueType, TTargetExtensionType>(keyName, defaultValue));
                return this;
            }

            public IReadOnlyCollection<ISettingKey> GetSettingKeys()
            {
                return _settingKeys.AsReadOnly();
            }
        }
    }
}
