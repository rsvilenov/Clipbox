using Clipbox.Core.Configuration;
using System.Collections.Generic;

namespace Clipbox.Infrastructure.Configuration
{
    public interface IConfigurationProvider
    {
        void RegisterSetting<T>(SettingKey<T> settingKey);

        void SetSetting<T>(SettingKey<T> settingKey, T value);

        T GetSetting<T>(SettingKey<T> settingKey);

        IReadOnlyCollection<string> GetAllSettingKeyNames();
    }
}
