using Clipbox.Core.Extensibility.Contracts.ExtensionTypes.GeneralTypes;

namespace Clipbox.Core.Extensibility.Contracts.ExtentionTypes.GeneralTypes.Builders
{
    /// <summary>
    /// Allows creating of a SettingKey collection.
    /// </summary>
    public interface IRegisteredSettingsStore
    {
        IRegisteredSettingsStore Add<TValueType, TTargetExtensionType>(string keyName, TValueType defaultValue)
            where TTargetExtensionType : ConfigurableExtension;
    }
}
