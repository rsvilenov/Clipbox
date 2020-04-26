using Clipbox.Core.Configuration;
using System.Collections.Generic;
using System.Windows;

namespace Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes
{
    /// <summary>
    /// Settings view extension
    /// </summary>
    public abstract class SettingsUIExtension : ConfigurableExtension, ISettingsRegistrator
    {
        private readonly IEnumerable<ISettingKey> _registeredSettingKeys;

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
        
        #region ISettingsRegistrator

        IEnumerable<ISettingKey> ISettingsRegistrator.GetSettingKeys()
        {
            return _registeredSettingKeys;
        }

        #endregion

        
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
