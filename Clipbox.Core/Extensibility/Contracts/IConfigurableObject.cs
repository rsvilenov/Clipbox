using Clipbox.Core.Configuration;

namespace Clipbox.Core.Extensibility.Contracts
{
    public interface IConfigurableObject
    {
        void AddSetting(string settingKeyName, SettingObject setting);

        void UpdateSetting(string settingKeyName, SettingObject setting);
    }
}
