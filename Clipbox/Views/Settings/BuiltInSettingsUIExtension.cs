using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Core.Extensibility.Contracts.ExtentionTypes;
using Clipbox.Core.Extensibility.Enums;
using Clipbox.Core.Extensibility.EventArgsClasses;
using Clipbox.Views.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Clipbox.Views.Settings
{
    public abstract class BuiltInSettingsUIExtension : 
        ISettingsUIComponent, 
        ISettingChangeRequestor, 
        IConfigurationRecipient, 
        ISettingsRegistrator,
        ICanRequestAppRestart
    {
        private readonly Dictionary<ISettingKey, ISettingUIPart> _settingKeyUIPartMapping;

        public BuiltInSettingsUIExtension()
        {
            _settingKeyUIPartMapping = new Dictionary<ISettingKey, ISettingUIPart>();
            BuiltInSettingsUIBuilder builder = new BuiltInSettingsUIBuilder();
            RegisterSettingUIParts(builder);
            _settingKeyUIPartMapping = builder.GetSettingKeyUIPartMapping();
        }

        /// <summary>
        /// The extensions override this method in order to know when
        /// the request made by calling of the method <see cref="RequestSettingChange"/> 
        /// has been fulfilled.
        /// </summary>
        /// <param name="settingKey">The key of the setting.</param>
        protected virtual void OnSettingUpdated(ISettingKey settingKey) { }

        /// <summary>
        /// Registers the UI elements which will control our settings.
        /// </summary>
        /// <param name="builder">This is where the UI elements are registered.
        /// Call <see cref="IBuiltInSettingsUIBuilder.Add(ISettingKey, ISettingUIPart)"/> 
        /// to register a UI part.</param>
        protected abstract void RegisterSettingUIParts(IBuiltInSettingsUIBuilder builder);

        /// <summary>
        /// Called when the extension receives a setting from the system during the initialization
        /// </summary>
        protected virtual void OnInitialSettingReceived(ISettingKey settingKey) { }

        /// <summary>
        /// Specifies the category, in which the setting will appear
        /// in the settings UI.
        /// </summary>
        protected abstract SettingsCategory Category { get; }

        /// <summary>
        /// Create and return the part of the Settings view,
        /// which shows a preview of how the settings will
        /// affect the functionning of their extensions.
        /// </summary>
        /// <param name="previewParams">The parameters passed to the preview generators</param>
        /// <returns>The preview (IPeviewData object) of the settings, which will be shown in the settings window</returns>
        protected virtual IPreviewData GetResultPreview(PreviewParams previewParams)
        { return null; }

        /// <summary>
        /// Force the preview to redraw. This will case the system to call
        /// <see cref="ISettingsUIComponent.GetResultPreviewData()"/> again.
        /// </summary>
        protected void InvalidateResultPreview()
        {
            _resultPreviewInvalidated?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// This goves the extensions the ability to request a restart of the app
        /// after a setting,which requires a restart, has been changed.
        /// </summary>
        protected void RequestAppRestart()
        {
            _restartAppRequested?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// This give the extensions the ability to request a change of settings
        /// directly from the SettingUIExtension, rather than from the UI Part.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        protected void RequestSettingChange(ISettingKey key, object newValue)
        {
            _requestSettingChange?.Invoke(this, new RequestSettingChangeEventArgs(key, newValue));
        }

        /// <summary>
        /// Gets the value of a setting.
        /// </summary>
        /// <typeparam name="TValueType">The type of the setting value.</typeparam>
        /// <param name="settingKey">The key of the setting</param>
        /// <returns>The value of the setting.</returns>
        protected TValueType GetSetting<TValueType>(ISettingKey settingKey)
        {
            lock (_lockObject)
            {
                if (!_settings.Any(s => s.Key == settingKey))
                {
                    throw new ArgumentException($"Setting with name '{settingKey.Name}' is not used by this extension.");
                }

                return (TValueType)_settings
                    .Single(s => s.Key == settingKey)
                    .Value;
            }
        }

        #region ISettingChangeRequestor
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
                        OnSettingUpdated(settingKey);
                    }
                }
                else
                {
                    _settings.Add(settingKey, settingValue);

                    OnInitialSettingReceived(settingKey);
                }
            }
        }
        #endregion

        #region ISettingsUIComponent
        private event EventHandler _resultPreviewInvalidated;
        /// <summary>
        /// <see cref="ISettingsUIComponent.ResultPreviewInvalidated"/>
        /// </summary>
        event EventHandler ISettingsUIComponent.ResultPreviewInvalidated
        {
            add
            {
                _resultPreviewInvalidated -= value;
                _resultPreviewInvalidated += value;
            }

            remove
            {
                _resultPreviewInvalidated -= value;
            }
        }

        SettingsCategory ISettingsUIComponent.GetCategory()
        {
            return Category;
        }

        void ISettingsUIComponent.SetUpSettingUIParts()
        {
            foreach (ISettingKey settingKey in _settingKeyUIPartMapping.Keys)
            {
                ISettingUIPart settingUIPart = _settingKeyUIPartMapping[settingKey];

                SetUpUpSettingUIPart(settingKey, settingUIPart);
            }
        }

        /// <summary>
        /// Returns the parts of the Settings view,
        /// corresponding to the settings for an extension.
        /// The parts are all set up and ready.
        /// </summary>
        /// <returns>The ui part to show in the settings windpw</returns>
        IReadOnlyDictionary<ISettingKey, ISettingUIPart> ISettingsUIComponent.GetSettingsViewParts()
        {
            return new ReadOnlyDictionary<ISettingKey, ISettingUIPart>(_settingKeyUIPartMapping);
        }

        IPreviewData ISettingsUIComponent.GetResultPreviewData(PreviewParams previewParams)
        {
            return GetResultPreview(previewParams);
        }
        #endregion

        #region ISettingsRegistrator
        public IEnumerable<ISettingKey> GetSettingKeys()
        {
            return _settingKeyUIPartMapping.Keys;
        }
        #endregion

        #region ICanRequestAppRestart
        private event EventHandler _restartAppRequested;
        /// <summary>
        /// <see cref="ICanRequestAppRestart.RestartAppRequested"/>
        /// </summary>
        event EventHandler ICanRequestAppRestart.RestartAppRequested
        {
            add
            {
                _restartAppRequested -= value;
                _restartAppRequested += value;
            }

            remove
            {
                _restartAppRequested -= value;
            }
        }
        #endregion

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

        
        private Dictionary<ISettingKey, object> _settings = new Dictionary<ISettingKey, object>();

        private readonly object _lockObject = new object();


        protected interface IBuiltInSettingsUIBuilder
        {
            IBuiltInSettingsUIBuilder Add(ISettingKey settingKey, ISettingUIPart uiPart);
        }

        private class BuiltInSettingsUIBuilder : IBuiltInSettingsUIBuilder
        {
            private readonly Dictionary<ISettingKey, ISettingUIPart> _parts;

            public BuiltInSettingsUIBuilder()
            {
                _parts = new Dictionary<ISettingKey, ISettingUIPart>();
            }

            public IBuiltInSettingsUIBuilder Add(ISettingKey settingKey, ISettingUIPart uiPart)
            {
                if (!(uiPart is ISettingUIPart))
                {
                    throw new ArgumentException($"{nameof(uiPart)} should implement {nameof(ISettingUIPart)}.");
                }

                _parts.Add(settingKey, uiPart);

                return this;
            }

            public Dictionary<ISettingKey, ISettingUIPart> GetSettingKeyUIPartMapping()
            {
                return _parts;
            }
        }

    }
}
