using Clipbox.Core.Configuration;

namespace Clipbox.Infrastructure.Configuration
{
    public interface IConfigurationProvider
    {
        void RegisterSetting(ISettingKey settingKey);

        bool IsSettingRegistered(ISettingKey settingKey);

        void SetSetting(ISettingKey settingKey, object value);

        object GetSetting(ISettingKey settingKey);
    }
}
