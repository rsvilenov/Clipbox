using Clipbox.Core.Configuration;
using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;
using System.Collections.Generic;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes.Builders
{
    /// <summary>
    /// We do not want the actual ISettingKey to be accessible by the extensions.
    /// They are an implementation detail and should not be exposed. So, construct
    /// that builder only here and provide a ready object to the method
    /// <see cref="RegisterSettings(IRegisteredSettingsStore)"/>
    /// The extensions can therefore register keys without constructing
    /// ISettingKey objects.
    /// </summary>
    internal class RegisteredSettingsStore : IRegisteredSettingsStore
    {
        private readonly List<ISettingKey> _settingKeys;

        public RegisteredSettingsStore()
        {
            _settingKeys = new List<ISettingKey>();
        }

        public IRegisteredSettingsStore Add<TValueType, TTargetExtensionType>(string keyName, TValueType defaultValue)
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
