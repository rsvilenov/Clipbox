using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Enums;
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
        public event EventHandler ResultPreviewInvalidated;

        private readonly Dictionary<ISettingKey, ISettingUIPart> _settingKeyUIPartMapping;

        public SettingsUIExtension()
        {
            var settingKeyColBuilder = new SettingKeyCollectionBuilder();
            RegisterSettings(settingKeyColBuilder);
            _registeredSettingKeys = settingKeyColBuilder.GetSettingKeys();
            
            _settingKeyUIPartMapping = new Dictionary<ISettingKey, ISettingUIPart>();
        }

        /// <summary>
        /// Specifies the category, in which the setting will appear
        /// in the settings UI.
        /// </summary>
        public virtual SettingsCategory Category { get; }

        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by calling of the method <see cref="RequestSettingChange"/> 
        /// has been fulfilled.
        /// </summary>
        /// <param name="settingName">The name of the setting.</param>
        /// <param name="targetType">The extension type, for which the value is registered.</param>
        protected virtual void OnSettingUpdated(string settingName, Type targetType) { }

        /// <summary>
        /// Returns the parts of the Settings view,
        /// corresponding to the settings for an extension.
        /// The parts are all set up and ready.
        /// </summary>
        /// <returns>The ui part to show in the settings windpw</returns>
        public IEnumerable<ISettingUIPart> GetSettingsViewParts()
        {
            SettingsUIBuilder builder = new SettingsUIBuilder(_settings);
            RegisterSettingUIParts(builder);
            Dictionary<ISettingKey, ISettingUIPart> mapping = builder.GetSettingKeyUIPartMapping();

            foreach (ISettingKey settingKey in mapping.Keys)
            {
                ISettingUIPart settingUIPart = mapping[settingKey];

                if (!_settings.Any(s => s.Key == settingKey))
                {
                    throw new InvalidOperationException($"No settings available for SettingUIPart {typeof(ISettingUIPart).Name}");
                }

                SetUpUpSettingUIPart(settingKey, settingUIPart);

                _settingKeyUIPartMapping.Add(settingKey, settingUIPart);

                yield return settingUIPart;
            }
        }

        /// <summary>
        /// Create and return the part of the Settings view,
        /// which shows a preview of how the settings will
        /// affect the functionning of their extensions.
        /// </summary>
        /// <param name="previewParams">The parameters passed to the preview generators</param>
        /// <returns>The preview (IPeviewData object) of the settings, which will be shown in the settings window</returns>
        public virtual IPreviewData GetResultPreview(PreviewParams previewParams)
        { return null; }

        /// <summary>
        /// Register the settings thet will be used by the consuming extensions.
        /// To register a setting, add it to the instance of <see cref="IRegisteredSettingStore"/>
        /// which is passed as a parameter to the medthod.
        /// </summary>
        protected abstract void RegisterSettings(IRegisteredSettingStore settingsStore);


        /// <summary>
        /// Registers the UI elements which will control our settings.
        /// </summary>
        /// <param name="builder">This is where the UI elements are registered.
        /// Call <see cref="ISettingsUIBuilder.Add(ISettingKey, ISettingUIPart)"/> 
        /// to register a UI part.</param>
        protected abstract void RegisterSettingUIParts(ISettingsUIBuilder builder);

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

        /// <summary>
        /// Allows for passing settings to an instance of the extension(s),
        /// for which this setting ui extensions provides configurability.
        /// Useful for providing setting preview using the actual preview generator.
        /// </summary>
        /// <typeparam name="TExtension">The type of the extension to receive the settings</typeparam>
        /// <param name="extension">The instance of the extension</param>
        protected void PassSettingsTo<TExtension>(TExtension extension)
            where TExtension : IConfigurationRecipient
        {
            foreach (ISettingKey key in _settings.Keys)
            {
                extension.PutSetting(key, _settings[key]);
            }
        }

        protected void InvalidateResultPreview()
        {
            ResultPreviewInvalidated?.Invoke(this, new EventArgs());
        }

        private void SetUpUpSettingUIPart(ISettingKey settingKey, ISettingUIPart settingUIPart)
        {
            object settingValue = _settings
                .First(s => s.Key == settingKey)
                .Value;
            settingUIPart.SetSettingValue(settingValue);
            
            settingUIPart.NotifySettingModified += SettingUIPart_NotifySettingModified;
        }

        private void SettingUIPart_NotifySettingModified(object sender, EventArgs e)
        {
            ISettingUIPart uiPart = (ISettingUIPart)sender;

            foreach (ISettingKey settingKey in _settingKeyUIPartMapping.Where(kvp => kvp.Value == uiPart).Select(kvp => kvp.Key))
            {
                if (!_settings.Any(s => s.Key == settingKey))
                {
                    throw new InvalidOperationException($"No settings available for SettingUIPart {typeof(ISettingUIPart).Name}");
                }

                _settings.FirstOrDefault(s => s.Key == settingKey);
                _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(settingKey, uiPart.GetSettingValue()));
            }
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

        /// <summary>
        /// Allows creating of a SettingKey collection.
        /// </summary>
        protected interface IRegisteredSettingStore
        {
            IRegisteredSettingStore Add<TValueType, TTargetExtensionType>(string keyName, TValueType defaultValue)
                where TTargetExtensionType : ConfigurableExtension;
        }

        /// <summary>
        /// We do not want the actual ISettingKey to be accessible by the extensions.
        /// They are an implementation detail and should not be exposed. So, construct
        /// that builder only here and provide a ready object to the method
        /// <see cref="RegisterSettings(IRegisteredSettingStore)"/>
        /// The extensions can therefore register keys without constructing
        /// ISettingKey objects.
        /// </summary>
        private class SettingKeyCollectionBuilder : IRegisteredSettingStore
        {
            private readonly List<ISettingKey> _settingKeys;

            public SettingKeyCollectionBuilder()
            {
                _settingKeys = new List<ISettingKey>();
            }

            public IRegisteredSettingStore Add<TValueType, TTargetExtensionType>(string keyName, TValueType defaultValue)
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

        protected interface ISettingsUIBuilder
        {
            ISettingsUIBuilder Add(string settingName, ISettingUIPart uiPart);
        }

        private class SettingsUIBuilder : ISettingsUIBuilder
        {
            private readonly Dictionary<ISettingKey, ISettingUIPart> _parts;
            private readonly IDictionary<ISettingKey, object> _settings;

            public SettingsUIBuilder(IDictionary<ISettingKey, object> settings)
            {
                _parts = new Dictionary<ISettingKey, ISettingUIPart>();
                _settings = settings;
            }

            public ISettingsUIBuilder Add(string settingName, ISettingUIPart uiPart)
            {
                if (!(uiPart is ISettingUIPart))
                {
                    throw new ArgumentException($"{nameof(uiPart)} should implement {nameof(ISettingUIPart)}.");
                }

                foreach (ISettingKey settingKey in _settings.Keys
                    .Where(k => k.Name == settingName
                        && uiPart.GetSettingValueType() == k.TypeOfValue))
                {

                    if (settingKey == null)
                    {
                        throw new ArgumentException($"{nameof(settingName)} : {settingName} of type {uiPart.GetSettingValueType().Name} is not registered.");
                    }

                    _parts.Add(settingKey, uiPart);
                }

                return this;
            }

            public Dictionary<ISettingKey, ISettingUIPart> GetSettingKeyUIPartMapping()
            {
                return _parts;
            }
        }

    }
}
