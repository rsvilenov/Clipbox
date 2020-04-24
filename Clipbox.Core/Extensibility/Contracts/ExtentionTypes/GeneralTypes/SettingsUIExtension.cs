using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.EventArgsClasses;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes
{
    /// <summary>
    /// Settings view extension
    /// </summary>
    public abstract class SettingsUIExtension : ConfigurableExtension,
        IConfigurationChangeRequestor, ISettingsRegistrator
    {

        private readonly IReadOnlyCollection<Type> _registeredConsumingExtensions;

        private readonly IEnumerable<ITargettedSettingKey> _registeredSettingKeys;

        public SettingsUIExtension()
        {
            var settingKeyColBuilder = new SettingKeyCollectionBuilder();
            RegisterSettingKeys(settingKeyColBuilder);
            _registeredSettingKeys = settingKeyColBuilder.GetSettingKeys();
        }
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
        /// Raises a request to change a setting. When the setting is changed,
        /// <see cref="ConfigurableExtension.OnSettingUpdated(SettingObject)"/> gets called, so that
        /// the requestor knows that the change has been applied.
        /// </summary>
        protected void RaiseRequestSettingChange<T>(string settingName, T value)
        {
            _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(settingName, value));
        }

        #region ISettingsRegistrator

        IEnumerable<ITargettedSettingKey> ISettingsRegistrator.GetSettingKeys()
        {
            return _registeredSettingKeys;
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

        protected class SettingKeyCollectionBuilder
        {
            private readonly List<ITargettedSettingKey> _settingKeys;

            public SettingKeyCollectionBuilder()
            {
                _settingKeys = new List<ITargettedSettingKey>();
            }

            public SettingKeyCollectionBuilder Add<TValueType, TTargetExtensionType>(string keyName, TValueType defaultValue)
            {
                _settingKeys.Add(new TargettedSettingKey<TValueType, TTargetExtensionType>(keyName, defaultValue));
                return this;
            }

            public IReadOnlyCollection<ITargettedSettingKey> GetSettingKeys()
            {
                return _settingKeys.AsReadOnly();
            }
        }
    }
}
